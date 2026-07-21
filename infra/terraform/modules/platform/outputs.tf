output "api_endpoint" {
  value = local.use_custom_domain ? "https://${var.domain_name}" : "http://${aws_lb.main.dns_name}"
}

output "addressables_domain" {
  value = aws_cloudfront_distribution.addressables.domain_name
}

output "ecr_repositories" {
  value = { for name, repository in aws_ecr_repository.service : name => repository.repository_url }
}
