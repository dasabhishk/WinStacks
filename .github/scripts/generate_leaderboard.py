import git
from collections import defaultdict
import os
import re

repo = git.Repo(".")

# --- Load .mailmap if exists ---
mailmap = {}
mailmap_path = os.path.join(repo.working_dir, ".mailmap")
if os.path.exists(mailmap_path):
    with open(mailmap_path, "r") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            # Format: Canonical Name <canonical@email> <alias@email>
            match = re.findall(r"<([^>]+)>", line)
            if len(match) >= 2:
                canonical_email = match[0]
                alias_emails = match[1:]
                for alias in alias_emails:
                    mailmap[alias] = canonical_email

# --- Collect stats ---
stats = defaultdict(lambda: {"added": 0, "removed": 0, "commits": 0, "pr_reviews": 0})

for commit in repo.iter_commits("main"):
    author_email = commit.author.email
    canonical_email = mailmap.get(author_email, author_email)
    author_name = commit.author.name

    key = f"{author_name} <{canonical_email}>"

    stats[key]["commits"] += 1
    diff = commit.stats.total
    stats[key]["added"] += diff["insertions"]
    stats[key]["removed"] += diff["deletions"]

# --- Sort leaderboard ---
sorted_stats = sorted(stats.items(), key=lambda x: x[1]["added"], reverse=True)

# --- Write leaderboard.md ---
with open("leaderboard.md", "w") as f:
    f.write("## 📊 Contributor Leaderboard\n\n")
    f.write("| Rank | Contributor | LOC Added | LOC Removed | Commits | PR Reviews |\n")
    f.write("|------|-------------|-----------|-------------|---------|------------|\n")

    for i, (contrib, data) in enumerate(sorted_stats, 1):
        f.write(
            f"| {i} | {contrib} | {data['added']} | {data['removed']} | {data['commits']} | {data['pr_reviews']} |\n"
        )
