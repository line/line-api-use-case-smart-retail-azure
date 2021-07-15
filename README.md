# Overview
[LINE API Use Caseサイト](https://lineapiusecase.com/ja/top.html)で提供している[スマートリテール](https://lineapiusecase.com/ja/usecase/smartretail.html)のデモアプリケーションソースコードとなります。    
今回紹介している手順を参考にすると、LINE APIを活用したスマホレジアプリケーションを開発することが可能です。   
スマートリテールアプリケーションを利用すると、LINEアプリ上で商品の購入と決済を行うことが出来ます。ユーザー自らレジ業務を行うため、店舗・店員の業務負荷軽減にもなります。   
さらに、決済後にLIFFアプリで取得したユーザーIDを利用し、LINEで販促メッセージを送信することも出来ます。

なお、このページで紹介しているソースコードの環境はMicrosoft Azureを利用しています。アプリケーションのデプロイ時にはAzure DevOps(Repos,Piplelines)を利用することを想定しています。  
※ [The English version document is here.](./docs/en/README_en.md)

### 公式ドキュメントの参考箇所
公式ドキュメントの以下の項目を完了させ、次の手順に進んでください。なお、既に導入済みのものは適宜飛ばして下さい。  
※本資料は 2021 年 6 月に作成しているため、最新の公式ドキュメントの内容と齟齬がある可能性があります。

1. [Azure CLI(Bash) のインストール](https://docs.microsoft.com/ja-jp/cli/azure/install-azure-cli)
1. [Azure DevOpsの開始](https://docs.microsoft.com/ja-jp/azure/devops/user-guide/sign-up-invite-teammates?view=azure-devops)

## バーコードスキャナーについて
本アプリケーションではバーコードスキャナーを使用しています。[quaggaJS](https://github.com/serratus/quaggaJS) と Scandit どちらかを選択して開発できるようになっています。
Scandit をご利用になる場合は [SCANDIT サイト](https://www.scandit.com/jp/)で Scandit License Key を取得してください。

# Getting Started / Tutorial
こちらの手順では、アプリケーション開発に必要な「LINEチャネル作成、アプリケーションデプロイ、バックエンド・フロントエンドの開発環境構築、動作確認」について説明します。
以下リンク先の手順を参考にし、本番環境（Azure）とローカル環境の構築を行ってください。

### [LINE チャネルの作成](./docs/jp/liff-channel-create.md)
### [本番(Azure)環境構築/デプロイ](./docs/jp/deployment.md)
### [バックエンドのデプロイ・開発環境構築](./docs/jp/backend-deployment.md)
### [フロントエンドの開発環境構築](./docs/jp/frontend-deployment.md)
***
### [動作確認](./docs/jp/validation.md)
***
# License
Smart Retailの全てのファイルは、条件なしで自由にご利用いただけます。
自由にdownload&cloneをして、LINE APIを活用した素敵なアプリケーションの開発を始めてください！
