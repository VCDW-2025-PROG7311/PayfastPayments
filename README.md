## PayFast Integration Activity Guide (ASP.NET Core Web API + Render Deployment)

This activity designed to expose you to the process of integrating PayFast into an ASP.NET Core Web API application. 
You will learn how to initiate payments, receive ITN (Instant Transaction Notification) messages, and persist transaction data to a JSON file.
You will also learn how to deploy the application to Render using Docker.

---
### Introduction
In this activity, you will:
- Build an ASP.NET Core Web API
- Use PayFast to simulate payment processing
- Store transaction details in a local JSON file
- Handle payment confirmation through the ITN system
- Deploy your project to the Render cloud platform

---

### How PayFast Integration Works
When a user initiates a payment using your API, they are redirected to PayFast’s payment gateway using an automatically submitted HTML form. PayFast handles the payment and notifies your application through a server-to-server callback known as an ITN (Instant Transaction Notification). Your application listens for this notification, verifies it, and updates the relevant transaction record in your system. This allows you to confirm and record payments securely.

1. The user triggers a payment from your app.
2. An HTML form is generated and auto-submitted to PayFast.
3. After payment, PayFast sends an ITN (a POST request) to your backend.
4. Your backend receives the ITN, parses the form data, and updates the transaction.

---

### Prerequisites
- .NET SDK 9.0 installed
- Docker installed and working
- GitHub account
- Render.com account
- PayFast Sandbox account

###
By now, you should not need any guidance - try to build this on your own!


























If really stuck - [click here](/Guides/StepsOutlined.md) - but I really would appreciate it if you do this yourself!
