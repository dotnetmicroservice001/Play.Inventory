# Play.Inventory - Inventory Microservice

The Inventory microservice manages the **item inventory for each user** in the Play Economy system. It keeps track of which **catalog items** are owned by which users, and how many of each item they have.

## 📦 Features

- Stores and updates inventory of **catalog items per user**
- Tracks item ownership, additions, and consumption
- Integrates with **Catalog**, **Trading**, and **Identity** services
- Reacts to events like item purchases or grants
- Secured via **OAuth 2.0** and **JWT tokens** from the Identity service

## 🧱 Tech Stack

- ASP.NET Core
- RabbitMQ + MassTransit (event-driven communication)
- OAuth 2.0 / OpenID Connect (Duende IdentityServer)


## Creating and Publishing Package 
```bash
version="1.0.2"
owner="dotnetmicroservice001"
gh_pat="[YOUR_PERSONAL_ACCESS_TOKEN]"

dotnet pack src/Play.Inventory.Contracts --configuration Release \
  -p:PackageVersion="$version" \
  -p:RepositoryUrl="https://github.com/$owner/Play.Inventory" \
  -o ../Packages
  
dotnet nuget push ../Packages/Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat \
--source "github"
```

## Build a Docker Image
```bash
export version="1.0.3"
export GH_OWNER=dotnetmicroservice001
export GH_PAT="ghp_YourRealPATHere"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t play.inventory:$version .
```

## Run Docker Container
```bash 
export version="1.0.3"
export cosmosDbConnString="conn string here"
export serviceBusConnString="conn string here"
docker run -it --rm \
  -p 5004:5004 \
  --name inventory \
  -e MongoDbSettings__ConnectionString=$cosmosDbConnString \
  -e ServiceBusSettings__ConnectionString=$serviceBusConnString \
  -e ServiceSettings__MessageBroker="SERVICEBUS" \
  play.inventory:$version
```