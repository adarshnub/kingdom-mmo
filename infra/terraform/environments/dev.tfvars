environment            = "dev"
aws_region             = "ap-south-1"
image_tag              = "replace-with-git-sha"
api_desired_count      = 1
realtime_desired_count = 1
worker_desired_count   = 1
database_instance      = "db.t4g.micro"
redis_node_type        = "cache.t4g.micro"
deletion_protection    = false

