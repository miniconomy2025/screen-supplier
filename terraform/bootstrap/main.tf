terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.16"
    }
  }
  required_version = ">= 1.2.0"
  backend "s3" {
    bucket = "terraform-state-bucket-screen-supplier-grp"
    key    = "bootstrap/terraform.tfstate"
    region = "af-south-1"
  }
}

provider "aws" {
  region = "af-south-1"
}

# Provider for ACM certificates (must be us-east-1 for CloudFront)
provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

# S3 bucket for Terraform state
resource "aws_s3_bucket" "terraform_state" {
  bucket = "terraform-state-bucket-screen-supplier-grp"  
  tags = {
    Name        = "Terraform State Bucket"
    Environment = "prod"
  }
}

resource "aws_s3_bucket_versioning" "terraform_state_versioning" {
  bucket = aws_s3_bucket.terraform_state.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "terraform_state_encryption" {
  bucket = aws_s3_bucket.terraform_state.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "terraform_state_block" {
  bucket = aws_s3_bucket.terraform_state.id
  block_public_acls   = true
  block_public_policy = true
  ignore_public_acls  = true
  restrict_public_buckets = true
}

# S3 bucket for frontend static hosting (private, served via CloudFront)
resource "aws_s3_bucket" "frontend" {
  bucket = "screen-supplier-frontend-static"
  tags = {
    Name        = "Frontend Static Website"
    Environment = "prod"
  }
}

# Keep bucket private, CloudFront will access via OAI
resource "aws_s3_bucket_public_access_block" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# CloudFront Origin Access Identity for S3
resource "aws_cloudfront_origin_access_identity" "frontend_oai" {
  comment = "OAI for screen-supplier frontend"
}

# S3 bucket policy to allow CloudFront access
resource "aws_s3_bucket_policy" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowCloudFrontAccess"
        Effect    = "Allow"
        Principal = {
          AWS = aws_cloudfront_origin_access_identity.frontend_oai.iam_arn
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.frontend.arn}/*"
      },
    ]
  })

  depends_on = [aws_s3_bucket_public_access_block.frontend]
}

# ACM Certificate for both subdomains (must be in us-east-1 for CloudFront)
resource "aws_acm_certificate" "main" {
  provider                  = aws.us_east_1
  domain_name              = "screen-supplier.projects.bbdgrad.com"
  subject_alternative_names = ["screen-supplier-api.projects.bbdgrad.com"]
  validation_method        = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = {
    Name = "screen-supplier-certificate"
  }
}

# CloudFront distribution for frontend
resource "aws_cloudfront_distribution" "frontend" {
  origin {
    domain_name = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_id   = "S3-${aws_s3_bucket.frontend.bucket}"

    s3_origin_config {
      origin_access_identity = aws_cloudfront_origin_access_identity.frontend_oai.cloudfront_access_identity_path
    }
  }

  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"

  # aliases = ["screen-supplier.projects.bbdgrad.com"]

  default_cache_behavior {
    allowed_methods  = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = "S3-${aws_s3_bucket.frontend.bucket}"

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
    min_ttl                = 0
    default_ttl            = 3600
    max_ttl                = 86400
    compress               = true
  }

  # Handle SPA routing
  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  price_class = "PriceClass_100"  # Use only North America and Europe

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
    # acm_certificate_arn = aws_acm_certificate.main.arn
    # ssl_support_method  = "sni-only"
  }

  tags = {
    Name = "screen-supplier-frontend"
  }
}

# Database secrets
resource "aws_secretsmanager_secret" "db-secrets" {
  name = "prod/postgres-screen-supplier"
}

resource "random_password" "db_password" {
  length  = 16
  special = true
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db-secrets.id
  secret_string = jsonencode({
    db_username = "postgres"
    db_password = random_password.db_password.result
  })
}

output "s3_website_endpoint" {
  description = "S3 website endpoint for frontend"
  value       = aws_s3_bucket.frontend.bucket_regional_domain_name
}

output "frontend_bucket_name" {
  description = "Frontend S3 bucket name"
  value       = aws_s3_bucket.frontend.bucket
}

output "frontend_cloudfront_domain" {
  description = "CloudFront domain for frontend"
  value       = aws_cloudfront_distribution.frontend.domain_name
}

output "acm_certificate_arn" {
  description = "ACM certificate ARN"
  value       = aws_acm_certificate.main.arn
}