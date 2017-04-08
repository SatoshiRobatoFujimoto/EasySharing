# EasySharing
HoloLensアプリケーションにて、お手軽にSharingを実現するサンプルです

## サンプルアプリの概要
* ユーザ一人ひとりに対して、コントロールできる3Dモデル(ユニティちゃん or クエリちゃん)が1体前方に出現します
* コントロール対象の3DモデルはHologramCollectionにアタッチしているEasySharingCharacterManagerのSelf Character Typeから選択できます
* 3Dモデルは位置の変更、回転、拡大、縮小の操作が可能です。操作方法は[ブログ記事](http://blog.d-yama7.com/archives/481)をご参照ください

## 導入
* [HoloToolKit-Unity](https://github.com/Microsoft/HoloToolkit-Unity)をプロジェクトにインポートする
* [SDユニティちゃん](http://unity-chan.com/)をダウンロードし、プロジェクトにインポートする
* [クエリちゃん SD版](https://www.assetstore.unity3d.com/jp/#!/content/35616)をプロジェクトにインポートする
* [HologramsLikeController.unitypackage](https://github.com/dykarohora/HologramsLikeController)をプロジェクトにインポートする
* EasySharing.unitypackageをプロジェクトにインポートする
* EasySharing/SampleScene/Mainを起動する
* ヒエラルキーからSharingを選択し、インスペクタからSharingService.exeが動いているサーバのIPを設定する
* コントロール対象の3DモデルはHologramCollectionにアタッチしているEasySharingCharacterManagerのSelf Character Typeから選択する
* アプリをビルドし、HoloLensに配置する
* Sharingするユーザは、物理的に同じ場所、同じ向きでアプリを起動する(理由は[ブログ記事](http://blog.d-yama7.com/archives/569)をご参照ください)

## 注意点
* 3人以上のSharingも可能ですが、用意しているキャラクターは2種類のみですので重複します
* 重複しても自分のキャラクターしか操作できません

## 参考
* アイデアについての説明は[ブログ記事](http://blog.d-yama7.com/archives/569)をご参照ください

## ライセンス
* MIT License

## ライセンス表記
SDユニティちゃんとクエリちゃんSD版を使用させていただきました。

![SDユニティちゃん](http://unity-chan.com/images/imageLicenseLogo.png)
![SDクエリちゃん](http://query-chan.com/wp-content/uploads/2016/08/02_%E3%82%AF%E3%82%A8%E3%83%AA%E3%81%A1%E3%82%83%E3%82%93%E3%83%A9%E3%82%A4%E3%82%BB%E3%83%B3%E3%82%B9%E3%83%AD%E3%82%B4-e1472646888241-300x256.png)