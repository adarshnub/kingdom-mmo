# AWS deployment

## Environments

Use separate AWS accounts for development, staging, and production when available. Each environment has independent Terraform state, secrets, network, databases, and deployment approvals. The default region is `ap-south-1`.

## Deployment sequence

1. CI validates code, contracts, migrations, Terraform, and container images.
2. Publish immutable API/realtime/worker images to ECR using the Git commit SHA.
3. Run an expand-compatible migration as a one-off ECS task with an advisory lock.
4. Deploy staging, run smoke and contract tests, then exercise one authoritative timer.
5. Deploy production with ECS rolling update and deployment circuit-breaker rollback.
6. Run post-deploy checks and monitor error rate, latency, database saturation, and job lag.
7. Remove old schema only in a later release after the rollback window closes.

## Data protection

- Production RDS uses Multi-AZ, encryption, deletion protection, 35-day automated backups, and point-in-time recovery.
- Take a manual snapshot before high-risk migrations and copy scheduled snapshots to a recovery region.
- Enable S3 versioning and lifecycle policies for content, reports, and Terraform state.
- Treat Redis as rebuildable; do not restore it over newer PostgreSQL state.
- Target RPO 15 minutes and RTO 60 minutes. Test restoration quarterly.

## Secrets and access

GitHub Actions assumes deployment roles through OIDC. Workloads use ECS task roles. Application secrets live in Secrets Manager and are encrypted with KMS. Production databases and cache nodes have no public route. Require MFA for human production access and audit every GM mutation.

## Rollback

ECS rolls back an unhealthy task set to the last completed deployment. Because migrations are backward compatible, rollback changes only image tags. For corrupted data, stop writes, restore RDS to a new instance, verify economy and purchase consistency, then change the application secret/endpoint through a controlled deployment.
