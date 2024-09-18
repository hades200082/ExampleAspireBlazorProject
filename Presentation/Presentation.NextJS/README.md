# NextJS web client

This directory contains the NextJS web client.

To use the NextJS client within your project, in the AppHost project set the following appSettings:

```json
{
  "WebClient": {
    "WebClientProvider": "NextJS",
    "ApiEnvironmentVariableName": "NEXT_PUBLIC_API_BASE_URL"
  }
}
```

Ensure that you run `npm install` in this directory to install the required node_modules.

## Running the client

Run the Aspire AppHost project to run the entire solution including this NextJS project.