import git
import os
import json
from collections import defaultdict

REPO_PATH = os.getcwd()
repo = git.Repo(REPO_PATH)

# Make sure .mailmap is applied (GitPython respects it if present)
contributors = defaultdict(lambda: {
    "loc_added": 0,
    "loc_removed": 0,
    "commits": 0,
    "pr_reviews": 0
})

print("🔄 Collecting contributor stats...")

for commit in repo.iter_commits():
    # Skip first commit (no parent)
    if not commit.parents:
        continue

    author = commit.author.name.strip()
    email = commit.author.email.strip()
    contributor_key = f"{author} <{email}>"

    try:
        diff = commit.stats.total  # total = {"insertions": x, "deletions": y, "lines": z}
        contributors[contributor_key]["loc_added"] += diff["insertions"]
        contributors[contributor_key]["loc_removed"] += diff["deletions"]
        contributors[contributor_key]["commits"] += 1
    except Exception as e:
        print(f"⚠️ Skipping commit {commit.hexsha}: {e}")
        continue

# Save leaderboard data
leaderboard = []
for i, (contrib, stats) in enumerate(
    sorted(contributors.items(), key=lambda x: (x[1]["loc_added"] - x[1]["loc_removed"]), reverse=True),
    start=1
):
    leaderboard.append({
        "rank": i,
        "contributor": contrib,
        "loc_added": stats["loc_added"],
        "loc_removed": stats["loc_removed"],
        "commits": stats["commits"],
        "pr_reviews": stats["pr_reviews"]
    })

# Output JSON for frontend to render
output_path = os.path.join(REPO_PATH, "leaderboard.json")
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(leaderboard, f, indent=2)

print(f"✅ Leaderboard generated: {output_path}")
