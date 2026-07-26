# Frozen v4 website

The v4 documentation and samples are preserved as an immutable GitHub release
asset. The Pages workflow downloads this snapshot and adds it to `website/v4`;
it never rebuilds the `develop/4.1` branch.

The release, source commit, checksum, and expected file count are pinned in
`pages-v4-snapshot.json`. The source commit is the last verified `gh-pages`
deployment before the migration to native GitHub Pages artifacts.

The release tag intentionally points to a parentless, empty metadata commit,
not to a commit in `main`. This prevents the infrastructure tag from being
selected by Git-based package-version calculation. Keep future frozen-site
release tags outside the `main` history for the same reason.

The snapshot was created with Git checkout conversion disabled so its contents
match the blobs published by GitHub:

```powershell
git -c core.autocrlf=false -c core.eol=lf archive `
  --format=tar.gz `
  --prefix=v4/ `
  --output=mapsui-v4-site.tar.gz `
  697b9a4bd790b3572bb8b4278e45c0b7e633f8de:v4
```

Replacing the snapshot is an exceptional migration operation. A replacement
must be reviewed, published under a new release tag, and accompanied by an
updated manifest.
