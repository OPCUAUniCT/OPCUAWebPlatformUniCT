# OPC UA Web Platform

OPC UA Web Platform is a web application based on ASP.NET Core 2. 
It provides a REST interface between OPC UA Servers and generic clients 
which have no knowledge about OPC UA specification. Furthermore, the communication
 is is stateless and no session must be maintained between clients and platform.
 Such clients could be represented by web browser or constrained IoT-device.

A generic client of the platform owns a reduced view of the Address Space 
handled by an OPC UA Server. Web Platform provides access to OPC UA Servers
exposing them as Data Set resources. Clients can access this Data Set in order 
to explore the Address Space as if this were a simple graph composed by nodes 
and edges.

Furthermore, web platform manage the monitoring of variables by means of 
publish/subscribe communication based on MQTT and/or SignalR.

## Features

OPC UA Web Platform exposes the following resources through a RESTful
interface:

Resource | Description
---------|------------
/authenticate | It defines a routine that return a JWT Token used by the platform in order to authenticate client's requests
/data-sets | A collection of all OPC UA Servers exposed by the platform.
/data-sets/{dataset-id}/nodes | Returns the entry point of the Address Space relevant to the Data Set with **{node-id}**. It redirect the request to **/data-sets/{dataset-id}/nodes/0-85** which is relevant to the node representing the standard OPC UA Node *Object*.
/data-sets/{dataset-id}/nodes/{node-id} | Returns the state of the node with **{node-id}**.
/data-sets/{dataset-id}/monitor | It defines a routine that takes a *node-id*, a *broker url* and a *topic* as inputs, and start publishing notification to the broker. Broker url must have a prefix relevant to the supported technology, e.g. "**mqtt**:localhost" or "**signalr**:http:localhost:8080"
/data-sets/{dataset-id}/stop-monitor | It defines a routine that takes *topic* and *broker url* as inputs and stop publishing notification relevant the provided couple <topc, broker url>.

Features currently being implemented by means of the REST interface are:

* Authentication with JWT Token
* Read state of nodes Variable, Object, Method
    * Read Variable's value of all Built-In Data Type for scalar, arrays and multidimensional arrays values.
    * Read Variable's value of all Standard Data Type for scalar and array values.
    * Read Variable's value of vendor-specific Structured Data Type for scalar and array values.
* Write new values for nodes Variable.
    * Write Variable's value of all Built-In Data Type for scalar, arrays and multidimensional arrays values.
    * Write Variable's value of all Standard Data Type for scalar and array values.
    * Write Variable's value of vendor-specific Structured Data Type for scalar and array values.
* Monitor Variable's value through Pubish/Subscribe communication based on MQTT and SignalR. Variables may support 3 kind of deadband notification (**None**, **Absolute**, **Percent**) as described in OPC UA specification. All variables exposed by the platform support at leas **None**

## Getting started

OPC UA Web Platfrom requires .NET Core 2, available for Windows, Linux and MacOS [here](https://www.microsoft.com/net/learn/get-started).

Start cloning the project in your local machine and configure it
in order to expose your OPC UA Servers through the OPC UA Web Platform interface:

1. Clone the project in a local folder:

    `git clone https://github.com/OPCUAUniCT/OPCUAWebPlatformUniCT`

2. Edit the application configuration file **appsettings.json** setting all
the information relevant to the OPC UA servers you want to be exposed by the platform

    ```json
    "OPCUAServersOptions": {
        "Servers": [
          {
            "Name": "UaCppLocalServer",
            "Url": "opc.tcp://localhost:48010"
          },
          {
            "Name": "Raspberry Server",
            "Url": "opc.tcp://192.168.1.101:48010"
          }
        ]
      }
    ```

    You can edit the configuration file in order to change JWT authentication parameter too.

3. Edit the file **OPCUAWebClatform.Config.xml** in order to set all the configuration
relevant to the OPC UA Middleware embedded in the platform (which is itself 
an OPC UA Client, so it requires its own configuration).

4. Go in the project directory and run the project

    `dotnet run`

    N.B. You have to take care about running the Web Platform in *Development* or 
*Production* configuration. You can choose the configuration setting the 
environment variable **ASPNETCORE_ENVIRONMENT**, as explained [here](https://docs.microsoft.com/it-it/aspnet/core/fundamentals/environments).

