return {
  title = "Guest Login",
  subtitle = "Create or restore a local guest account",
  primary_action = "Enter Kingdom",
  primary_target = "auth",
  panels = {
    "Calls POST /v1/auth/guest with a persistent installation id.",
    "Loads GET /v1/bootstrap after the token is issued.",
  },
}
