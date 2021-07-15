# Overview
This is the demo application source code for [Smart Retail](https://lineapiusecase.com/en/usecase/smartretail. html) provided on the [LINE API Use Case site](https://lineapiusecase.com/en/top.html). html) provided on the [LINE API Use Case site]().    
By referring to the steps described in this article, you can develop a smart retail application that utilizes the LINE API.   
With the smart retail application, users can purchase products and make payments on the LINE application. Since the users themselves perform the checkout process, it also reduces the workload of stores and store staff.   
Furthermore, after the payment is made, the user ID obtained from the LIFF application can be used to send promotional messages via LINE.

Please note that the source code environment introduced in this page uses Microsoft Azure. It is assumed that Azure DevOps (Repos, Piplelines) will be used when deploying the application.  

※ [日本語ドキュメントはこちらからご確認いただけます。](../../README.md)

### Reference points in the official documentation
This document was created in June 2021, so there may be discrepancies with the latest official documentation.

1. [Installing Azure CLI(Bash)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
1. [Start Azure DevOps](https://docs.microsoft.com/en-us/azure/devops/user-guide/sign-up-invite-teammates?view=azure-devops)

## SCANDIT License
This application uses a barcode scanner, and you can choose to develop with either Quagga or Scandit.
If you want to use Scandit, please go to [SCANDIT site](https://www.scandit.com/) and get the Scandit License Key.

# Getting Started / Tutorial
In this tutorial, you will learn how to create a LINE channel, deploy the application, build the back-end and front-end development environment, and check the operation of the application.
Please refer to the steps in the following links to build your production environment (Azure) and local environment.

### [Create a LINE channel](./liff-channel-create.md)
### [Build/deploy production (Azure) environment](./deployment.md)
### [Backend deployment/development environment creation](./backend-deployment.md)
### [Building the frontend development environment](./frontend-deployment.md)
***
### [Operational validation](./validation.md)

# License
All files in SmartRegister are free to use without any conditions.
Feel free to download&clone them and start developing great applications using the LINE API!
