# CI/CD Setup Guide for Fly.io Deployment

This guide explains how to set up automatic deployment to Fly.io when you push to the `main` branch.

## Overview

The GitHub Actions workflow (`.github/workflows/fly-deploy.yml`) automatically:
1. Runs your test suite
2. Deploys to Fly.io (if tests pass)
3. Provides deployment logs and status

## One-Time Setup

### Step 1: Get Your Fly API Token

Run this command in your terminal:

```bash
fly auth token
```

This will output your personal Fly.io API token. **Copy this token** - you'll need it in the next step.

> **Security Note:** This token grants access to your Fly.io account. Keep it secure and never commit it to your repository.

### Step 2: Add Token to GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter the following:
   - **Name:** `FLY_API_TOKEN`
   - **Secret:** (paste the token from Step 1)
5. Click **Add secret**

That's it! You're done with the one-time setup.

## Usage

### Automatic Deployment

Simply push your code to the `main` branch:

```bash
git add .
git commit -m "Your commit message"
git push origin main
```

The workflow will automatically:
1. Run tests
2. Deploy to Fly if tests pass
3. Report status in the GitHub Actions tab

### Manual Deployment

You can also trigger deployment manually from GitHub:

1. Go to your repository on GitHub
2. Click the **Actions** tab
3. Select **Deploy to Fly.io** workflow
4. Click **Run workflow** button
5. Select the branch you want to deploy
6. Click **Run workflow**

This is useful for:
- Deploying hotfixes from a feature branch
- Re-deploying without making a new commit
- Testing the CI/CD pipeline

## Monitoring Deployments

### GitHub Actions

View deployment status and logs:
1. Go to **Actions** tab in your GitHub repo
2. Click on the latest workflow run
3. Expand the "Run Tests" and "Deploy to Fly" jobs to see logs

### Fly.io Logs

After deployment completes, check your app logs:

```bash
fly logs
```

Or view in the Fly.io dashboard: https://fly.io/dashboard

## Troubleshooting

### "FLY_API_TOKEN secret not found"

**Problem:** The workflow fails with an error about missing `FLY_API_TOKEN`.

**Solution:** 
- Make sure you added the secret in GitHub: Settings → Secrets and variables → Actions
- Secret name must be exactly `FLY_API_TOKEN` (case-sensitive)
- Re-run the workflow after adding the secret

### Tests are Failing

**Problem:** Deployment doesn't happen because tests fail.

**Solution:**
- Check the test logs in the GitHub Actions tab
- Fix the failing tests locally: `dotnet test`
- Commit and push the fix

### Deployment Fails

**Problem:** Tests pass but deployment to Fly fails.

**Solution:**
- Check if your `fly.toml` is correct
- Verify the app exists: `fly apps list`
- Check Fly status: https://status.flycdn.net/
- Review deployment logs in GitHub Actions

### Want to Skip Tests?

**Not recommended**, but if you need to deploy urgently:

1. Temporarily comment out the `needs: test` line in `.github/workflows/fly-deploy.yml`
2. Or, manually deploy from your local machine: `fly deploy`

## Advanced: Deploy Tokens (Optional)

For better security, create a deploy-only token instead of using your personal token:

```bash
fly tokens create deploy -a ticket-masala
```

This creates a token scoped only to the `ticket-masala` app. Update the `FLY_API_TOKEN` secret with this new token.

**Benefits:**
- More secure (limited to one app)
- Can be revoked independently
- Recommended for production

## Workflow Details

The workflow file (`.github/workflows/fly-deploy.yml`) contains two jobs:

### 1. Test Job
- Runs on: Every push to main
- Uses: .NET 10
- Steps: Restore → Build → Test

### 2. Deploy Job
- Runs on: Only if tests pass
- Uses: Fly CLI
- Steps: Checkout → Setup Fly → Deploy

## Customization

### Deploy to Different Branches

To deploy from other branches, edit `.github/workflows/fly-deploy.yml`:

```yaml
on:
  push:
    branches: [main, staging, production]  # Add your branches
```

### Skip CI for Certain Commits

Add `[skip ci]` to your commit message:

```bash
git commit -m "Update README [skip ci]"
```

The workflow won't run for this commit.

## Next Steps

- Push your code to start automatic deployments!
- 📊 Add a CI/CD badge to your README (optional)
- Set up deployment notifications in Slack/Discord (optional)
- 🌿 Configure preview deployments for pull requests (optional)

## Questions?

- GitHub Actions docs: https://docs.github.com/actions
- Fly.io CI/CD guide: https://fly.io/docs/app-guides/continuous-deployment-with-github-actions/
