# =====================================================================
#  爆走ショッピング 開発環境セットアップ
#    使い方: setup-dev.cmd をダブルクリック
#            （または） powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-dev.ps1
#    1 回実行すれば OK。何度実行しても問題ありません。
#
#  設定される内容（その人自身の環境にだけ効きます）
#    1. 生成物をコミット前に止めるフック   (core.hooksPath = .githooks)
#    2. シーン / Prefab の自動マージ        (merge.unityyamlmerge)
# =====================================================================

$ErrorActionPreference = 'Continue'

$root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
Set-Location $root

Write-Host ''
Write-Host '=========================================='
Write-Host ' 爆走ショッピング 開発環境セットアップ'
Write-Host '=========================================='
Write-Host "リポジトリ: $root"
Write-Host ''

# ---- git リポジトリか確認 ------------------------------------------
git rev-parse --is-inside-work-tree *> $null
if ($LASTEXITCODE -ne 0) {
  Write-Host 'エラー: ここは git リポジトリではありません。' -ForegroundColor Red
  Write-Host 'setup-dev.cmd がリポジトリの中にあるか確認してください。'
  exit 1
}

# ---- 1) コミット前チェックのフック ----------------------------------
git config --local core.hooksPath .githooks
$hookFile = Join-Path $root '.githooks\pre-commit'
if ((git config --local --get core.hooksPath) -eq '.githooks' -and (Test-Path $hookFile)) {
  Write-Host '[1/2] OK  フックを有効にしました（Logs/obj/.vs や .meta 忘れをコミット前に止めます）' -ForegroundColor Green
} else {
  Write-Host '[1/2] NG  フックを有効にできませんでした（.githooks/pre-commit を確認）' -ForegroundColor Yellow
}

# ---- 2) Unity の SmartMerge -----------------------------------------
# Unity プロジェクトのフォルダ（ProjectSettings\ProjectVersion.txt がある場所）を探す
$versionFile = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
  ForEach-Object { Join-Path $_.FullName 'ProjectSettings\ProjectVersion.txt' } |
  Where-Object { Test-Path $_ } |
  Select-Object -First 1

$unityVersion = $null
if ($versionFile) {
  $m = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
  if ($m) { $unityVersion = $m.Matches[0].Groups[1].Value }
}
if ($unityVersion) { Write-Host "プロジェクトの Unity バージョン: $unityVersion" }

$hubRoot = 'C:\Program Files\Unity\Hub\Editor'
$mergeTool = $null
$useLocal = $false

if ($unityVersion) {
  $candidate = Join-Path $hubRoot "$unityVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
  if (Test-Path $candidate) { $mergeTool = $candidate }
}
if (-not $mergeTool) {
  $mergeTool = Get-ChildItem $hubRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'Editor\Data\Tools\UnityYAMLMerge.exe' } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1
  if ($mergeTool) { $useLocal = $true }
}

$includes = @(git config --local --get-all include.path)

if ($mergeTool -and -not $useLocal) {
  if ($includes -notcontains '../.gitconfig-unity') {
    git config --local include.path ../.gitconfig-unity
  }
  Write-Host '[2/2] OK  シーン / Prefab の自動マージを設定しました（UnityYAMLMerge）' -ForegroundColor Green
} elseif ($mergeTool) {
  Write-Host "     注意: Unity $unityVersion が見つからないので、別バージョンの SmartMerge を使います" -ForegroundColor Yellow
  Write-Host "           $mergeTool"
  $localCfg = Join-Path $root '.git\unity-merge.local'
  $body = @'
# setup-dev.ps1 が自動生成したこの PC 用の設定
[merge "unityyamlmerge"]
	name = Unity SmartMerge
	driver = \"{0}\" merge -p \"$BASE\" \"$REMOTE\" \"$LOCAL\" \"$MERGED\"
'@ -f ($mergeTool -replace '\\', '/')
  [System.IO.File]::WriteAllText($localCfg, $body, (New-Object System.Text.UTF8Encoding($false)))
  if ($includes -notcontains '../.git/unity-merge.local') {
    git config --local include.path ../.git/unity-merge.local
  }
  Write-Host '[2/2] OK  自動マージを設定しました（この PC 専用の設定ファイルを使用）' -ForegroundColor Green
} else {
  Write-Host '[2/2] NG  UnityYAMLMerge.exe が見つかりませんでした' -ForegroundColor Yellow
  Write-Host "          Unity Hub で Unity $unityVersion をインストールしてから、もう一度実行してください。"
}

# ---- 結果 -----------------------------------------------------------
Write-Host ''
Write-Host '--- 設定の確認 ---'
Write-Host ('core.hooksPath              = ' + (git config --local --get core.hooksPath))
Write-Host ('merge.unityyamlmerge.driver = ' + (git config --get merge.unityyamlmerge.driver))
Write-Host ''
Write-Host '完了です。以後は Logs/obj/.vs などの生成物や、.meta を付け忘れたアセットを'
Write-Host 'コミットしようとすると、その場で止まります。'
Write-Host ''
