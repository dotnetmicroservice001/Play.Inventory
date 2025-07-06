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
export appname="playeconomy-01"
export acrname="playeconomy01acr"

docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$acrname.azurecr.io/play.inventory:$version" .
```

## Run Docker Container
```bash 
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

## Publishing Docker Image
```bash 
az acr login --name $acrname
docker push "$acrname.azurecr.io/play.inventory:$version"
```

## Create Kubernetes namespace
```bash 
export namespace="inventory"
kubectl create namespace $namespace 
```

## Create the Kubernetes Pod
```bash
kubectl apply -f ./kubernetes/${namespace}.yaml -n "$namespace"
```

## Creating Azure Managed Identity and granting it access to Key Vault Store
```bash
export appname=playeconomy-01
az identity create --resource-group $appname --name $namespace 

export IDENTITY_CLIENT_ID=$(az identity show -g "$appname" -n "$namespace" --query clientId -o tsv)
export SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az role assignment create \
  --assignee "$IDENTITY_CLIENT_ID" \
  --role "Key Vault Secrets User" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$appname/providers/Microsoft.KeyVault/vaults/$appname"
```

## Establish the related Identity Credential
```bash
export AKS_OIDC_ISSUER="$(az aks show -n $appname -g $appname --query "oidcIssuerProfile.issuerUrl" -otsv)"
az identity federated-credential create --name ${namespace} --identity-name "${namespace}" --resource-group "${appname}" --issuer "${AKS_OIDC_ISSUER}" --subject system:serviceaccount:"${namespace}":"${namespace}-serviceaccount" --audience api://AzureADTokenExchange
```

---
