environment            = "prod"
aws_region             = "ap-south-1"
image_tag              = "replace-with-git-sha"
api_desired_count      = 2
realtime_desired_count = 2
worker_desired_count   = 2
database_instance      = "db.r7g.large"
redis_node_type        = "cache.r7g.large"
deletion_protection    = true

