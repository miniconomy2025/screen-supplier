terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.16"
    }
  }

  required_version = ">= 1.2.0"

  backend "s3" {
    bucket         = "terraform-state-bucket-screen-supplier-grp"
    key            = "env/prod/terraform.tfstate"
    region         = "af-south-1"
    encrypt        = true
  }
}

data "terraform_remote_state" "bootstrap" {
  backend = "s3"
  config = {
    bucket = "terraform-state-bucket-screen-supplier-grp"
    key    = "bootstrap/terraform.tfstate"  
    region = "af-south-1"
  }
}

data "aws_secretsmanager_secret" "db-secrets" {
  name = "prod/postgres-screen-supplier"
}

data "aws_secretsmanager_secret_version" "db" {
  secret_id = data.aws_secretsmanager_secret.db-secrets.id
}

locals {
  secret_data = jsondecode(data.aws_secretsmanager_secret_version.db.secret_string)
}

provider "aws" {
  region  = "af-south-1"
}

data "aws_ami" "ubuntu" {
  most_recent = true

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }

  owners = ["099720109477"]
}

resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true  
  enable_dns_support   = true  
}

resource "aws_subnet" "private_1" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.1.0/24"
  availability_zone = "af-south-1a"
}

resource "aws_subnet" "private_2" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.2.0/24"
  availability_zone = "af-south-1b"
}

resource "aws_subnet" "public" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.0.0/24"
  availability_zone       = "af-south-1a"
  map_public_ip_on_launch = true
}

resource "aws_subnet" "public_2" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.4.0/24"
  availability_zone       = "af-south-1b"
  map_public_ip_on_launch = true
}

resource "aws_db_subnet_group" "private-group" {
  name       = "screen-supplier-private-group"
  subnet_ids = [aws_subnet.private_1.id, aws_subnet.private_2.id]
  tags = {
    Name = "Screen Supplier Private subnet group"
  }
}

resource "aws_db_subnet_group" "public-group" {
  name       = "screen-supplier-public-group"
  subnet_ids = [aws_subnet.public.id, aws_subnet.public_2.id]
  tags = {
    Name = "Screen Supplier Public subnet group"
  }
}

resource "aws_internet_gateway" "gw" {
  vpc_id = aws_vpc.main.id
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.gw.id
  }
}

resource "aws_route_table_association" "a" {
  subnet_id      = aws_subnet.public.id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table_association" "public_2" {
  subnet_id      = aws_subnet.public_2.id
  route_table_id = aws_route_table.public.id
}

resource "aws_security_group" "ec2-security-group" {
  name        = "screen-supplier-ec2"
  description = "Allow API and web traffic"
  vpc_id      = aws_vpc.main.id

  ingress {
    description = "HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTPS from anywhere"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "SSH"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]  # Consider restricting this to your IP
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "screen-supplier-ec2-sg"
  }
}

resource "aws_security_group" "rds-security-group" {
  name        = "screen-supplier-rds"
  description = "Allow EC2 to talk to RDS"
  vpc_id      = aws_vpc.main.id
  
  ingress {
    description     = "PostgreSQL from EC2"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.ec2-security-group.id]
  }

  ingress {
    description = "PostgreSQL from anywhere (remove after setup)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "screen-supplier-rds-sg"
  }
}

resource "aws_db_instance" "postgres" {
  allocated_storage      = 20
  engine                 = "postgres"
  engine_version         = "15"
  instance_class         = "db.t3.micro"
  publicly_accessible    = true
  username               = local.secret_data.db_username
  password               = local.secret_data.db_password
  parameter_group_name   = "default.postgres15"
  skip_final_snapshot    = true
  db_name                = "ScreenProducerDb"
  vpc_security_group_ids = [aws_security_group.rds-security-group.id]
  db_subnet_group_name   = aws_db_subnet_group.public-group.name
  
  tags = {
    Name = "screen-supplier-rds"
  }
}

resource "aws_iam_role" "ec2_secrets_role" {
  name = "screen_supplier_ec2_secrets_role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Action = "sts:AssumeRole",
      Effect = "Allow",
      Principal = {
        Service = "ec2.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_policy" "secrets_access_policy" {
  name = "ScreenSupplierSecretsManagerAccessPolicy"

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Effect = "Allow",
      Action = [
        "secretsmanager:GetSecretValue"
      ],
      Resource = "*"
    }]
  })
}

resource "aws_iam_role_policy_attachment" "attach_secrets_policy" {
  role       = aws_iam_role.ec2_secrets_role.name
  policy_arn = aws_iam_policy.secrets_access_policy.arn
}

resource "aws_iam_instance_profile" "ec2_instance_profile" {
  name = "screen_supplier_ec2_instance_profile"
  role = aws_iam_role.ec2_secrets_role.name
}

resource "aws_instance" "app_server" {
  ami                    = data.aws_ami.ubuntu.id
  instance_type          = "t3.micro"
  subnet_id              = aws_subnet.public.id
  iam_instance_profile   = aws_iam_instance_profile.ec2_instance_profile.name
  key_name               = "screen-supplier-key"
  vpc_security_group_ids = [aws_security_group.ec2-security-group.id]

  tags = {
    Name = "screen-supplier-backend"
  }

  user_data = base64encode(<<-EOF
    #!/bin/bash
    set -e
    
    # Update system
    apt-get update -y
    apt-get install -y nginx awscli jq

    # Install .NET 9 runtime
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    apt-get update -y
    apt-get install -y aspnetcore-runtime-9.0

    # Configure nginx as reverse proxy
    cat > /etc/nginx/sites-available/default << 'NGINX_EOF'
server {
    listen 80 default_server;
    listen [::]:80 default_server;

    # Increase buffer sizes for large requests
    client_max_body_size 10M;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}
NGINX_EOF

    systemctl restart nginx
    systemctl enable nginx

    # Create app directory
    mkdir -p /home/ubuntu/build
    chown ubuntu:ubuntu /home/ubuntu/build
    
    echo "EC2 instance setup completed" > /var/log/user-data.log
  EOF
  )
}

resource "aws_secretsmanager_secret" "db_hostname" {
  name = "screen-supplier-rds-host"
}

resource "aws_secretsmanager_secret_version" "db_credentials_version" {
  secret_id     = aws_secretsmanager_secret.db_hostname.id
  secret_string = aws_db_instance.postgres.address
}

output "ec2_public_ip" {
  description = "EC2 instance public IP"
  value       = aws_instance.app_server.public_ip
}

output "ec2_public_dns" {
  description = "EC2 instance public DNS"
  value       = aws_instance.app_server.public_dns
}

output "rds_endpoint" {
  description = "RDS database endpoint"
  value       = aws_db_instance.postgres.endpoint
}

output "api_url" {
  description = "Backend API URL (use this in your frontend)"
  value       = "http://${aws_instance.app_server.public_dns}"
}

output "frontend_cloudfront_domain" {
  description = "CloudFront domain for frontend (from bootstrap)"
  value       = "Get from: terraform output -state=../bootstrap/terraform.tfstate frontend_cloudfront_domain"
}
