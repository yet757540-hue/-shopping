# 爆走ショッピング

Unity 製アクションゲーム「爆走ショッピング」のリポジトリです。
日々の操作は GitHub Desktop で完結します（追加でインストールするものはありません）。

## リポジトリ構成

```
-shopping/                        ← リポジトリのルート
├── 爆走ショッピング/              ← Unity プロジェクト本体（Unity で開くのはここ）
│   ├── Assets/
│   ├── Packages/
│   ├── ProjectSettings/
│   └── .gitignore
├── setup-dev.cmd                 ← 最初にこれをダブルクリック
├── setup-dev.ps1
├── .gitconfig-unity              ← UnityYAMLMerge の設定
├── .githooks/pre-commit          ← コミット前チェック
├── .gitattributes
└── .github/PULL_REQUEST_TEMPLATE.md
```

Unity プロジェクトは **リポジトリ直下の `爆走ショッピング/` サブフォルダ**にあります。
リポジトリのルート（`爆走ショッピング/` の一つ上の階層）を Unity で開かないでください。

## 動作環境

| 項目 | 値 |
| --- | --- |
| Unity | **6000.4.2f1**（このバージョンで作業してください） |
| レンダリング | Universal Render Pipeline (URP) |
| 入力 | Input System / Starter Assets |
| Git | GitHub Desktop に同梱のもので OK（別途インストール不要） |

Unity Hub の「Add project from disk」で `爆走ショッピング/` を指定して開きます。
`Library/` は各自の環境で生成されるためリポジトリには含まれません（初回オープンには数分かかります）。

## 最初にやること（クローンしたら 1 回だけ）

追加でダウンロードするものはありません。必要なファイルはすべてリポジトリに入っています。

1. Unity と Visual Studio を**閉じる**
2. GitHub Desktop で **Fetch origin → Pull origin**（初回は Clone）
3. メニューの **Repository → Show in Explorer** でフォルダを開き、**`setup-dev.cmd` をダブルクリック**
4. `[1/2] OK` と `[2/2] OK` が出れば完了

これで次の 2 つが設定されます（その人自身の環境にだけ効きます）。

1. シーン / Prefab を自動マージする UnityYAMLMerge（`.gitconfig-unity` を読み込む）
2. 生成物をコミット前に止めるフック（`core.hooksPath = .githooks`）

**この手順を飛ばした場合**: `.gitignore` はリポジトリ側で自動的に効くので、`Logs/` `obj/` `.vs/` などが GitHub Desktop の変更一覧に出ることはありません。
飛ばしたときに失われるのは、**`.meta` 忘れ・巨大ファイルのチェック**と、**シーン / Prefab の自動マージ**だけです。

コマンドで確認したいとき:

```powershell
git config --local --get core.hooksPath                 # → .githooks
git config --get merge.unityyamlmerge.driver             # → UnityYAMLMerge.exe のパス
```

（2 つ目だけ `--local` を付けないのは、設定を include で読み込んでいるためです。
`--local` を付けると git が include を展開せず、空が表示されてしまいます）

新しい PC やクローンし直したときは、もう一度実行してください（設定は clone ごとです）。

### 手動で設定する場合

```powershell
git config --local core.hooksPath .githooks
git config --local include.path ../.gitconfig-unity
```

Unity のインストール先が標準（`C:\Program Files\Unity\Hub\Editor\<バージョン>\`）でない場合は、
`.gitconfig-unity` の中の exe パスを自分の環境に合わせて書き換えてください。

## ブランチ運用

リモートにあるのは `main` だけです（`DEV` / `dennya` / `ぬ` / `仮調整` は整理済み）。

| ブランチ | 役割 |
| --- | --- |
| `main` | 常に動く状態。**直接 push しない**。PR 経由でのみ更新する |
| `feature/<内容>` | 機能追加・作業用。例: `feature/player-model` |
| `fix/<内容>` | 不具合修正用。例: `fix/dash-collision` |

1. `main` から作業ブランチを切る
2. 作業して push する
3. PR を作成する（テンプレートに沿って動作確認を書く）
4. レビュー後にマージし、**マージ済みの作業ブランチは削除する**

マージ済みのブランチを残さないでください。「どれが最新か分からない」状態が一番事故を生みます。

## コミットメッセージ

`<種類>: <内容>` の形式で書きます。

- `feat: プレイヤーモデルを差し替え`
- `fix: ダッシュ中に壁をすり抜ける問題を修正`
- `art: レジのモデルを追加`
- `scene: タイトルシーンに BGM を設定`
- `docs: README を追加`
- `chore: 生成物を追跡から除外`

`1` `いろいろ` `test` のようなメッセージは避けてください。後から原因を追えなくなります。

## コミットしてはいけないもの

以下は Unity / IDE が自動生成するもので、**コミットすると全員の作業と衝突します**。

- `Library/` `Temp/` `obj/` `Logs/` `UserSettings/` `ProfilerCaptures/`
- `.vs/` `*.csproj` `*.sln` `*.suo`
- ビルド成果物（`Build/` や、`*.exe` を含む配布フォルダ一式）
- 50MB を超えるファイル

`.gitignore` で除外済みで、さらに `.githooks/pre-commit` がコミット自体を止めます。

## `.meta` は必ずセットでコミットする

Unity は `.meta` に書かれた GUID でアセット同士の参照を解決します。
**アセットを追加したのに `.meta` をコミットし忘れると、他のメンバーの環境で参照が全部切れます。**
（フォルダにも `.meta` が必要です）

フックが「`.meta` が含まれていないファイル」を検出してコミットを止めます。
引っかかったら、Unity で一度インポートして `.meta` を生成し、アセットと一緒にステージしてください。

## シーン・Prefab の同時編集について

`.unity` / `.prefab` は中身が YAML ですが、二人が同時に同じファイルを編集するとほぼ確実に競合します。

- 同じシーン / Prefab を同時に触らない（声を掛け合う）
- 編集が終わったらすぐ push する
- 競合したら手で YAML を直さず、UnityYAMLMerge（SmartMerge）を使う

## 困ったとき

**pull できない（`local changes would be overwritten`）**
`Logs/` `obj/` `*.csproj` など自動生成されるファイルのローカル変更が邪魔をしています。
GitHub Desktop の変更一覧でそのファイルを右クリック → Discard changes で捨てるか、ファイルごと削除してから pull してください。Unity を開き直せば再生成されます。

**コミットしようとしたら「コミットを中止しました」と出た**
フックが止めています。表示された内容に応じて対処してください。

| 表示 | 対処 |
| --- | --- |
| 生成物（`Logs/` `obj/` `.vs/` `*.csproj` など） | 追跡から外す（`git rm --cached -- <パス>`）|
| `.meta` が含まれていない | Unity でインポートして `.meta` を生成し、アセットと一緒に選ぶ |
| 50MB を超えるファイル | Git LFS か外部ストレージを検討する（相談してください）|

GitHub Desktop からは回避できないので、表示に従って直してください。

**Unity のバージョンが違うと言われた**
`setup-dev.cmd` は Unity Hub のインストール先から自動で探します。6000.4.2f1 が無い場合は別バージョンの SmartMerge を使い、その旨を表示します。まったく見つからない場合は Unity Hub からインストールしてください。

**古い clone をそのまま使っている**
`main` を pull すれば追いつきます。ローカルに `DEV` ブランチが残っていても害はありません（不要なら `git branch -d DEV`）。
pull のときに `Logs/` `obj/` `.vs/` `UserSettings/` や古い `*.csproj` / `*.sln` が削除されますが、Unity / Visual Studio を開き直せば再生成されます。

## 大きなファイル（LFS）

`.gitattributes` に Git LFS の設定をコメントで用意してあります。
PSD / TIF / FBX / 音声などを本格的に追加する前に、LFS の有効化を相談してください。
**一度コミットした大きなファイルは、後から削除しても履歴に残り続けます。**

## リポジトリのメンテナンス（管理者向け）

### マージ済みブランチの削除

```powershell
git branch --merged main              # main にマージ済みの一覧
git branch -d <ブランチ名>             # ローカルを削除
git push origin --delete <ブランチ名>  # リモートを削除
```

### `.git` が肥大化したとき

**不可逆な操作です。** どのブランチからも辿れない古いオブジェクトが消えます。
実行前にリポジトリ全体をバックアップするか、新しいクローンを作っておいてください。

```powershell
git count-objects -vH        # 現在のサイズを確認
git fsck --unreachable       # 消える対象を確認（任意）

git reflog expire --expire=now --all
git gc --prune=now
```

### Git LFS を有効化するとき

```powershell
git lfs install --local
# .gitattributes の LFS 行（コメントアウト済み）を有効化してから
git lfs migrate import --include="*.psd,*.tif,*.fbx,*.wav"
```

### 既知の課題

- 過去にビルド成果物・ログ・旧プロジェクト（`seisaku(kari)/`）がコミットされたため、履歴が大きくなっています。
  整理する場合は全員の再 clone が必要になるので、必ず事前に相談してください。
- 作業フォルダを OneDrive の同期対象に置くと、`Camera 1.meta` のような**重複 `.meta` が混入する事故**が実際に起きています（2026-09 に 24 個を削除）。
  可能なら `C:\Git\` など OneDrive の外に置いてください。
