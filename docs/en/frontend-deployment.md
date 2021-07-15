# Configure the frontend deployed to Azure
Deployment is executed in a pipeline, so there is no special work to do

# Development environment / Local development environment
## IDE
You can use whatever you like, but we recommend VSCode if you are not particular about it.

Visual Studio Code  
https://code.visualstudio.com

## PHP, http-server
This is used to set up a local server.  

http-server  
https://www.npmjs.com/package/http-server

## ngrok
Use ngrok to display local LIFF apps.
Install ngrok  
https://ngrok.com/download

## node installation
Prepare node to build a JavaScriot environment.  
Install the LTS version from https://nodejs.org/en/download/

## Install the node package
1. navigate to [frontend folder](../../frontend/) on the command line (e.g. Terminal).
1. do `npm install`.
1. install `npm` in [frontend folder](../../frontend/), a directory named `node_modules` will be created.

## Run locally using ngrok.
  - [.env](../../frontend/.env) environment variables to the appropriate values
    - Change `BASE_URL` to the Static Web Apps URL
    - Change the value of `LIFF_ID` by getting the liffId from the official LINE account.
  - Change `BASE_URL` to the appropriate value and set up a local server.
    - Use `php -S localhost:5000` or `http-server -p 5000`.
  - Publish the URL of the public server using [ngrok](https://ngrok.com/).
      - `ngrok http 5000`.
        - Set the endpoint URL of the LIFF application to the URL issued by ngrok.
  - After starting functions, go to the LIFF URL ![liff-url](../images/en/liff-url.png)

[Go to next page](validation.md)

[Back to Table of Contents](./README_en.md)
