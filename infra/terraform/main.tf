module "platform" {
  source = "./modules/platform"

  environment            = var.environment
  aws_region             = var.aws_region
  image_tag              = var.image_tag
  api_desired_count      = var.api_desired_count
  realtime_desired_count = var.realtime_desired_count
  worker_desired_count   = var.worker_desired_count
  database_instance      = var.database_instance
  redis_node_type        = var.redis_node_type
  deletion_protection    = var.deletion_protection
  domain_name            = var.domain_name
  route53_zone_id        = var.route53_zone_id
}
