# Migration from `master` to `main` Branch

This PR updates all references from `master` to `main` branch throughout the repository.

## What Has Been Done

✅ **All GitHub Actions workflow files updated** to reference `main` instead of `master`:
- `android-build.yml` - Updated trigger branches and PR targets
- `copilot-agent-solver.yml` - Updated PR base branch
- `copilot-automated-development-cycle.yml` - Updated trigger branches
- `dotnet-integration-tests.yml` - Updated trigger branches
- `dotnet-test-grid.yml` - Updated trigger branches and conditional checks
- `full-build.yml` - Updated trigger branches
- `github-models-integration-test.yml` - Updated trigger branches and conditionals
- `ionos-deploy.yml` - Updated trigger branches
- `mutation-testing.yml` - Updated `--since:` parameter
- `ollama-integration-test.yml` - Updated trigger branches
- `terraform-infrastructure.yml` - Updated trigger branches
- `terraform-tests.yml` - Updated trigger branches

✅ **All conditional branch checks updated**:
- `github.ref == 'refs/heads/main'` (was `refs/heads/master`)
- Branch conditionals in shell scripts updated

## What You Need to Do

After merging this PR, you need to complete the migration by creating the `main` branch and setting it as default:

### Step 1: Create the `main` branch from `master`

**Option A: Using GitHub Web UI (Recommended)**
1. Go to your repository on GitHub
2. Click on the branch dropdown (currently showing `master`)
3. Type `main` in the text box
4. Click "Create branch: main from master"

**Option B: Using Git Command Line**
```bash
git fetch origin
git checkout -b main origin/master
git push -u origin main
```

### Step 2: Set `main` as the default branch

1. Go to your repository Settings on GitHub
2. Navigate to "Branches" section (left sidebar)
3. Under "Default branch", click the switch icon or pencil icon next to `master`
4. Select `main` from the dropdown
5. Click "Update" and confirm the change

### Step 3: Update protected branch rules (if applicable)

If you have branch protection rules for `master`, you should:
1. Go to Settings → Branches → Branch protection rules
2. Add the same protection rules for `main`
3. Optionally, you can delete the protection rule for `master` after verifying `main` works

### Step 4: Verify the migration

1. Check that new PRs are now targeting `main` by default
2. Verify that CI/CD workflows run correctly on the `main` branch
3. Ensure all team members update their local repositories:
   ```bash
   git fetch origin
   git branch -m master main
   git branch -u origin/main main
   ```

### Step 5: (Optional) Archive or delete `master` branch

Once you've verified everything works on `main`:
1. You can optionally keep `master` for a grace period
2. Or delete it from GitHub: Settings → Branches → Delete `master` branch
3. Communicate to your team that `master` is deprecated

## Impact Analysis

- ✅ All GitHub Actions will now trigger on `main` instead of `master`
- ✅ New PRs will target `main` by default
- ✅ Submodules already use `main` as their default branch (no changes needed)
- ✅ No changes needed to submodule configurations in `.gitmodules` (they already track `main`)

## Notes

- The submodules (`.build/`, `foundation/`, `engine/`, `app/`) already use `main` as their default branch
- This change aligns the meta-repository with the submodule branch naming convention
- All workflow conditionals checking for `master` have been updated to check for `main`
