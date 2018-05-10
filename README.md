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


![webplatformarc](https://user-images.githubusercontent.com/25839693/39619258-7a3c8c9c-4f87-11e8-91d2-cd2590fa7979.PNG)

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

You can more information in the [API Documentation](https://documenter.getpostman.com/view/1090837/opc-ua-web-platform/RVfyAUSR)

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

## Rid of DataTypes

The powerful concept of the OPC UA Web Platform is the lack of knowledge about OPC UA protocol specification
of the client. So that, a client have no understanding about DataTypes of the OPC UA standard and relies only on
the meaning of the JSON base types. 

In order to enable the client to interact with an OPC UA Server anyway, the OPC UA platform provides
a JSON schema for all the data values exposed by an OPC UA Server. That is, a client is able 
to validate (and understand description) of values and write new well-formed values 
to the platform.

All schema concerning values of Variable Nodes will be contained in a field **"value-schema"** of
the response body, as will be shown in the following examples.

## Getting started

OPC UA Web Platfrom requires .NET Core 2, available for Windows, Linux and MacOS [here](https://www.microsoft.com/net/learn/get-started).

Start cloning the project in your local machine and configure it
in order to expose your OPC UA Servers through the OPC UA Web Platform interface:

1. Clone the project in a local folder:

    `git clone https://github.com/OPCUAUniCT/OPCUAWebPlatformUniCT`

2. Edit the application configuration file **appsettings.json** setting all
the information relevant to the OPC UA servers you want to be exposed by the platform

    ```js
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

### Run on Docker container

It si possible running OPC UA Web Platform on Docker container with the following command

`docker run --rm -it -p 5000:80 marsala/opcua-web-platform:1.0.0`

### Troubleshooting

You may occur in error like "DataSet Not Available" even if all the ip addresses or your OPC UA Server are perfectly configured. Be aware you have configured the platform **Instance Certificate** in the OPC UA Servers **Trusted** certificate store.

## Examples

In the following will be highlighted some common use cases for the OPC UA Web Platform. Remember that all
request to the Api with the initial enpoint **/api/** require an authentication token. A valid token can be
obtained with a simple request to the following API endpoint:

`POST http://{{base_url}}/auth/authenticate`

The response will contain a valid token that must be included in an header **"Authorization"** like shown
in the following examples.

This is an example request of authentication using the default Authentication service mock:

```
curl -X POST \
  http://localhost:5000/auth/authenticate \
  -F username=admin \
  -F password=password
```

### Read the standard OPC UA Server *Object* Node

Suppose that the DataSet with dataset-id = 3 correspond to the OPC UA Server exposed by the platform
you are interested. So, if you want to start browsing the data set you have to make a request like 
the following:

`GET http://{{base_url}}/api/data-sets/3/nodes`

You can make a request with:

```
curl -X GET http://{{YOUR-URL}}:5000/api/data-sets/2/nodes 
  -H 'Authorization: Bearer {YOUR-AUTHENTICATION-TOKEN}'
```

It will return the response:

```js
{
    "node-id": "0-85",
    "name": "Objects",
    "type": "folder",
    "edges": [
        {
            "node-id": "0-2253",
            "name": "Server",
            "Type": "object",
            "relationship": "Organizes"
        },
        {
            "node-id": "3-BuildingAutomation",
            "name": "BuildingAutomation",
            "Type": "folder",
            "relationship": "Organizes"
        },
        {
            "node-id": "2-Demo",
            "name": "Demo",
            "Type": "folder",
            "relationship": "Organizes"
        },
        {
            "node-id": "5-Demo",
            "name": "DemoUANodeSetXML",
            "Type": "folder",
            "relationship": "Organizes"
        }
    ]
}
```

### Read the a Node

Read the state of a Variable Node:

`GET http://{{base_url}}/api/data-sets/3/nodes/2-Demo.Static.Scalar.WorkOrder`

You can make a request with:

```
curl -X GET \
  http://{YOUR-URL}/api/data-sets/3/nodes/2-Demo.Static.Scalar.WorkOrder \
  -H 'Authorization: Bearer {YOUR-AUTHENTICATION-TOKEN}' \
```

It will return the response:

```js
{
    "node-id": "2-Demo.Static.Scalar.WorkOrder",
    "name": "WorkOrder",
    "type": "variable",
    "value": {
        "ID": "9240890a-6ea8-41fc-8e84-f47edd3e3595",
        "AssetID": "123-X-Y-Z",
        "StartTime": "2018-04-20T14:24:39.085941Z",
        "NoOfStatusComments": 3,
        "StatusComments": [
            {
                "Actor": "Wendy Terry",
                "Timestamp": "2018-04-20T14:24:39.085941Z",
                "Comment": {
                    "Locale": "en-US",
                    "Text": "Mission accomplished!"
                }
            },
            {
                "Actor": "Gavin Mackenzie",
                "Timestamp": "2018-04-20T14:24:39.085941Z",
                "Comment": {
                    "Locale": "en-US",
                    "Text": "I think clients would love this."
                }
            },
            {
                "Actor": "Phil Taylor",
                "Timestamp": "2018-04-20T14:24:39.085941Z",
                "Comment": {
                    "Locale": "en-US",
                    "Text": "And justice for all."
                }
            }
        ]
    },
    "value-schema": {
        "type": "object",
        "properties": {
            "ID": {
                "type": "string"
            },
            "AssetID": {
                "type": "string"
            },
            "StartTime": {
                "type": "string"
            },
            "NoOfStatusComments": {
                "type": "number"
            },
            "StatusComments": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "Actor": {
                            "type": "string"
                        },
                        "Timestamp": {
                            "type": "string"
                        },
                        "Comment": {
                            "type": "object",
                            "properties": {
                                "Locale": {
                                    "type": "string"
                                },
                                "Text": {
                                    "type": "string"
                                }
                            }
                        }
                    }
                },
                "minItems": 3,
                "maxItems": 3
            }
        }
    },
    "status": "Good",
    "deadBand": "None",
    "minimumSamplingInterval": 0,
    "edges": []
}
```

### Write a new value for a Variable Node

Update the value of a Variable Node:

`POST http://{{base_url}}/api/data-sets/3/nodes/2-Demo.Static.Scalar.WorkOrder { "value": <YOUR-NEW-VALUE> }`

You can make a request with:

```
curl -X POST \
  http://{YOUR-URL}/api/data-sets/3/nodes/2-Demo.Static.Scalar.WorkOrder \
  -H 'Authorization: Bearer {YOUR-AUTHENTICATION-TOKEN} \
  -d '{
  "value": {
        "ID": "9240890a-6ea8-41fc-8e84-f47edd3e3595",
        "AssetID": "123-X-Y-Z",
        "StartTime": "2018-04-20T14:24:39.085941Z",
        "NoOfStatusComments": 2,
        "StatusComments": [
            {
                "Actor": "Dylan Thomas",
                "Timestamp": "2018-04-20T14:24:39.085941Z",
                "Comment": {
                    "Locale": "en-US",
                    "Text": "Do not go gentle into that good night"
                }
            },
            {
                "Actor": "William Shakespeare",
                "Timestamp": "2018-04-20T14:24:39.085941Z",
                "Comment": {
                    "Locale": "en-US",
                    "Text": "There is nothing either good or bad, but thinking makes it so."
                }
            }
        ]
    }
}'
```

It worth noting how a client not compliant with the OPC UA specification is able to write new well-formed values
with the aid of JSON Schemas.

## Papers

The foundation ideas of OPC UA Web Platform are discussed in the following papers. The platform described in these papers 
may differs for some aspects but the concepts are quite similar:

- [Integration of OPC UA into a web-based platform to enhance interoperability (ISIE 2017)](https://ieeexplore.ieee.org/document/8001417/)
- [OPC UA integration into the web (IECON 2017)](https://ieeexplore.ieee.org/document/8216590/)
- [A web-based platform for OPC UA integration in IIoT environment (ETFA 2017)](https://ieeexplore.ieee.org/document/8247713/)
