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

# Get bootstrap outputs
data "terraform_remote_state" "bootstrap" {
  backend = "s3"
  config = {
    bucket = "terraform-state-bucket-screen-supplier-grp"
    key    = "bootstrap/terraform.tfstate"  # Bootstrap state path
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

# Provider for CloudFront (requires us-east-1)
provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
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
  enable_dns_hostnames = true  # This enables public DNS names
  enable_dns_support   = true  # This was already there
}

resource "aws_subnet" "public" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.0.0/24"
  availability_zone = "af-south-1a"
  map_public_ip_on_launch = true
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

resource "aws_db_subnet_group" "private-group" {
  name       = "screen-supplier-private-group"
  subnet_ids = [aws_subnet.private_1.id, aws_subnet.private_2.id]
  tags = {
    Name = "Screen Supplier Private subnet group"
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

resource "aws_security_group" "ec2-security-group" {
  name        = "screen-supplier-ec2"
  description = "Allow API and web traffic"
  vpc_id      = aws_vpc.main.id

  ingress{
    from_port = 5000
    to_port = 5000
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]  
  }

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
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
    cidr_blocks = ["0.0.0.0/0"] 
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "rds-security-group" {
    name = "screen-supplier-rds"
    description = "Allow EC2 to talk to RDS"
    vpc_id      = aws_vpc.main.id
    ingress{
        from_port = 5432
        to_port = 5432
        protocol = "tcp"
        cidr_blocks = ["0.0.0.0/0"] 
    }
}

resource "aws_db_instance" "postgres" {
  allocated_storage    = 20
  engine               = "postgres"
  engine_version       = "15"
  instance_class       = "db.t3.micro"
  publicly_accessible = true
  username             = local.secret_data.db_username
  password             = local.secret_data.db_password
  parameter_group_name = "default.postgres15"
  skip_final_snapshot  = true
  db_name              = "ScreenProducerDb"
  vpc_security_group_ids = [aws_security_group.rds-security-group.id]
  db_subnet_group_name   = aws_db_subnet_group.private-group.name
  
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
  ami  = data.aws_ami.ubuntu.id
  instance_type = "t3.micro"
  subnet_id = aws_subnet.public.id
  iam_instance_profile = aws_iam_instance_profile.ec2_instance_profile.name
  tags = {
    Name = "screen-supplier-backend"
  }
  key_name = "screen-supplier-key"  # You'll need to create this key pair
  vpc_security_group_ids = [aws_security_group.ec2-security-group.id]

  # Install .NET runtime and nginx
  user_data = base64encode(<<-EOF
    #!/bin/bash
    apt-get update -y
    apt-get install -y nginx awscli

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
    }
}
NGINX_EOF

    systemctl restart nginx
    systemctl enable nginx

    # Create app directory
    mkdir -p /home/ubuntu/build
    chown ubuntu:ubuntu /home/ubuntu/build
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
  value = aws_instance.app_server.public_ip
}

output "api_cloudfront_domain" {
  description = "CloudFront domain for API"
  value       = aws_cloudfront_distribution.api.domain_name
}

output "frontend_cloudfront_domain" {
  description = "CloudFront domain for frontend"
  value       = "Get from bootstrap terraform output"
}

# CloudFront distribution for API
resource "aws_cloudfront_distribution" "api" {
  origin {
    domain_name = aws_instance.app_server.public_dns
    origin_id   = "EC2-${aws_instance.app_server.id}"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  enabled = true

  # Comment out custom aliases for now
  # aliases = ["screen-supplier-api.projects.bbdgrad.com"]

  default_cache_behavior {
    allowed_methods  = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods   = ["GET", "HEAD", "OPTIONS"]
    target_origin_id = "EC2-${aws_instance.app_server.id}"

    forwarded_values {
      query_string = true
      headers      = ["*"]
      cookies {
        forward = "all"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
    min_ttl                = 0
    default_ttl            = 0     # Don't cache API responses
    max_ttl                = 0
    compress               = false
  }

  # Don't cache API endpoints by default
  ordered_cache_behavior {
    path_pattern     = "/api/*"
    allowed_methods  = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods   = ["GET", "HEAD", "OPTIONS"]
    target_origin_id = "EC2-${aws_instance.app_server.id}"

    forwarded_values {
      query_string = true
      headers      = ["*"]
      cookies {
        forward = "all"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
    min_ttl                = 0
    default_ttl            = 0
    max_ttl                = 0
    compress               = false
  }

  price_class = "PriceClass_100"

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
    # Use default certificate for now - add custom domain later
    # acm_certificate_arn = aws_acm_certificate.main.arn
    # ssl_support_method  = "sni-only"
  }

  tags = {
    Name = "screen-supplier-api"
  }
}