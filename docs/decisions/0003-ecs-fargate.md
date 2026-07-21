# ADR-0003: ECS Fargate deployment

Status: Accepted

Deploy containers on ECS Fargate rather than Kubernetes. Use RDS PostgreSQL, ElastiCache, S3, and CloudFront. This provides independent scaling and managed rollback without requiring a Kubernetes control plane or cluster operations expertise.
