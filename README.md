![Splash screen preview](https://github.com/BepInEx/BepInEx.SplashScreen/assets/39247311/07831558-91e7-48fa-a2de-fc3c6d29a731)

# SuperNewRoles向けのBepInEx SplashScreen
- ローカライズし、SuperNewRolesに関する情報を表示するようにしたバージョンです。


# BepInEx 読み込み進捗スプラッシュ
ゲーム起動時にパッチャーとプラグインの読み込み状況を表示するBepInExパッチャーです。パッチャーやプラグインの初期化に時間がかかるゲームに最適です。

このパッチャーは主に、大規模なMODパックのエンドユーザーに即座にフィードバックを提供するために作られました。heavily modされたゲームを起動すると、特に低スペックのシステムでは、ゲームウィンドウが表示されたり反応するまでに時間がかかることがあり、ユーザーがゲームがクラッシュしたと誤解する可能性があります。

このパッチャーとGUIアプリは[risk-of-thunder/BepInEx.GUI](https://github.com/risk-of-thunder/BepInEx.GUI)の非常に古いバージョンから発展したものですが、現時点ではコードの大部分が書き直され、すべてのゲームで動作するようになっています。ただし、Risk Of Rain 2をMODする場合は、より良い体験のためにrisk-of-thunder/BepInEx.GUIを使用することをお勧めします。

## 使用方法
1. [BepInEx](https://github.com/BepInEx/BepInEx) 5.4.11以降、または6.0.0-be.674以降（monoとIL2CPPの両方で動作）をインストールします。
2. お使いのBepInExバージョンに対応した最新リリースをダウンロードします。
3. パッチャーファイルが`BepInEx\patchers`内に配置されるようにリリースを展開します。
4. BepInExが正しく設定されていれば、ゲーム起動時にスプラッシュ画面が表示されるはずです。

### スプラッシュ画面が表示されない場合
1. `BepInEx.SplashScreen.GUI.exe`と`BepInEx.SplashScreen.Patcher.dll`の両方が`BepInEx\patchers`フォルダ内に存在することを確認してください。
2. `BepInEx\config\BepInEx.cfg`でスプラッシュ画面が無効になっていないか確認してください。このファイルやSplashScreen Enableの設定が見つからない場合、BepInExが正しく設定されていないか、このパッチャーが何らかの理由で起動に失敗している可能性があります。
3. BepInEx 5を最新バージョンに更新し、正常に動作していることを確認してください。
4. それでもスプラッシュ画面が表示されない場合は、ゲームログでエラーや例外がないか確認してください。問題は[GitHub](https://github.com/BepInEx/BepInEx.SplashScreen/issues)で報告できます。

## 貢献
気軽にissueを立てていただき、PRも大歓迎です！貢献は https://github.com/BepInEx/BepInEx.SplashScreen のリポジトリに提出してください。

変更について議論したり、他のMOD開発者と話し合うには、[公式BepInEx Discordサーバー](https://discord.gg/MpFEDAg)をご利用ください。

## コンパイル方法
リポジトリをクローンし、Visual Studio 2022（.NETデスクトップ開発と.NET 3.5開発ツールがインストールされているもの）で.slnを開きます。`ソリューションのビルド`を実行すれば、そのまま動作するはずです。
