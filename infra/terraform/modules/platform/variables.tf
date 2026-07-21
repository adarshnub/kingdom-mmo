variable "environment" { type = string }
variable "aws_region" { type = string }
variable "image_tag" { type = string }
variable "api_desired_count" { type = number }
variable "realtime_desired_count" { type = number }
variable "worker_desired_count" { type = number }
variable "database_instance" { type = string }
variable "redis_node_type" { type = string }
variable "deletion_protection" { type = bool }
variable "domain_name" {
  type     = string
  default  = null
  nullable = true
}
variable "route53_zone_id" {
  type     = string
  default  = null
  nullable = true
}
