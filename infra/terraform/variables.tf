variable "environment" {
  description = "Deployment environment name."
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "aws_region" {
  description = "AWS region for Kingdom 1."
  type        = string
  default     = "ap-south-1"
}

variable "image_tag" {
  description = "Immutable Git SHA container tag."
  type        = string
}

variable "api_desired_count" {
  type    = number
  default = 1
}

variable "realtime_desired_count" {
  type    = number
  default = 1
}

variable "worker_desired_count" {
  type    = number
  default = 1
}

variable "database_instance" {
  type    = string
  default = "db.t4g.medium"
}

variable "redis_node_type" {
  type    = string
  default = "cache.t4g.small"
}

variable "deletion_protection" {
  type    = bool
  default = true
}

variable "domain_name" {
  description = "Optional public API DNS name, for example api.kingdom.example."
  type        = string
  default     = null
  nullable    = true
}

variable "route53_zone_id" {
  description = "Route 53 hosted zone for domain_name. Required when domain_name is set."
  type        = string
  default     = null
  nullable    = true
}
