# IEEE VIS Run of Show
This Run of Show Web App was originally developed for IEEE VIS 2022 and 2023 to provide technicians and session hosts with a detailed script of the respective session.

# Setting up the App

The `appsettings.json` file is used to configure various settings for the IEEE VIS Run of Show Web App. This file contains configuration data in JSON format.

## Location

The `appsettings.json` file is located in the `VisRunOfShow/VisRunOfShowWebApp` directory.

## Current appsettings.json

Current fields required by the `appsettings.json` file:

```json
{
    "CustomSettings": {
        "SheetsLink": "IEEE-Vis-Data-Sheet",
        "PrivateKey": "your-private-key",
        "AdminKey": "your-admin-key"
    }
}
```
# Running the application locally using Docker

In order to run the application using Docker please ensure that you Docker installed.

Next you can execute the shell using bash or sh

```sh
bash docker_build.sh
```

# URL to access the application
If the program executes successfully, the application would be available to view at this url: https://localhost:7133

# License

This project is MIT-licensed. Please refer to the LICENSE file in the root directory.

Copyright © Johannes Knittel
