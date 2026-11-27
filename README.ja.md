# VRCPA for Unity

[![GitHub release](https://img.shields.io/github/release/VRCPA/VRCPA_For_Unity.svg)](https://github.com/VRCPA/VRCPA_For_Unity/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/VRCPA/VRCPA_For_Unity/total?color=6451f1)](https://github.com/VRCPA/VRCPA_For_Unity/releases/latest)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/VRCPA/VRCPA_For_Unity/ci.yml?branch=master)](https://github.com/VRCPA/VRCPA_For_Unity/actions/workflows/ci.yml)
[![Discord](https://img.shields.io/badge/discord-join-7289DA?logo=discord&logoColor=white)](https://discord.gg/zJNsnX5q7Q)

[English](README.md) · [简体中文](README.zh-CN.md) · **日本語**

これは [VRC Photo Atelier](https://github.com/VRCPA/VRC-Photo-Atelier) の Unity 用サブリポジトリです。

## インストールとセットアップ

1. [Releases](https://github.com/VRCPA/VRCPA_For_Unity/releases/latest) から `.unitypackage` をダウンロードし、Unity プロジェクトにインポートします。

   ![VRCPA の Unity パッケージをインポート](image_for_readme/6.png)

2. Project ウィンドウの `Assets/VRCPA` にある `VRCPA` プレハブを、Hierarchy でアバターのルートオブジェクトの直下に配置します（例では `Plum_kisekae` という名前のアバターを使用しています）。

   ![Project ウィンドウの VRCPA プレハブ](image_for_readme/1.png)

   ![Hierarchy でアバターの直下に配置した VRCPA](image_for_readme/2.png)

3. Hierarchy で `VRCPA` を選択し、Inspector の **自動検出** をクリックします。

4. **VirtualLens2 オブジェクト** と **カメラ姿勢 (CameraPOS)** の両方に値が入ったら、**外部ポーズ参照を設定** をクリックします。Inspector に「VirtualLens2 にこの CameraPOS が設定されています。」と表示されれば、セットアップは完了です。

   ![VRCPA の Inspector にある「自動検出」と「外部ポーズ参照を設定」ボタン](image_for_readme/7.png)

   ![CameraPOS の設定が完了した VRCPA の Inspector](image_for_readme/3.png)

## 自動検出できないとき

- Hierarchy で `VRCPA` 配下の `CameraPOS` を、Inspector の **カメラ姿勢 (CameraPOS)** 欄にドラッグします。
- Hierarchy で **Virtual Lens Settings** コンポーネントが付いた VirtualLens2 オブジェクトを、Inspector の **VirtualLens2 オブジェクト** 欄にドラッグします。

![Hierarchy の VRCPA 配下にある CameraPOS](image_for_readme/4.png)

両方の欄に値を入れたら、**外部ポーズ参照を設定** をクリックします。

## 設定による変更

この操作では、VirtualLens2 の **External Pose Source** の参照先だけが VRCPA の `CameraPOS` に変わります。変更前のポーズ参照先に依存する他のギミックの動作に影響する場合があります。

![External Pose Source に CameraPOS が設定された VirtualLens2](image_for_readme/5.png)

## パフォーマンス

VRCPA が追加する同期パラメーターは 0 個で、ローカルの float パラメーターを 7 個使用します。

## 対応

このパッケージは VirtualLens2 に対応しています。

## 依存関係

- Modular Avatar
- VirtualLens2
