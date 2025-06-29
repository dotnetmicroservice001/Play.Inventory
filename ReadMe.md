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

dotnet pack Play.Inventory.Contracts --configuration Release \
  -p:PackageVersion="$version" \
  -p:RepositoryUrl="https://github.com/$owner/Play.Inventory" \
  -o ../Packages
  
dotnet nuget push ../Packages/Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat \
--source "github"
```