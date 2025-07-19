# VRP Solver SaaS – Proof of Concept

This repository contains an academic proof-of-concept for a scalable, value-for-money **Vehicle Routing Problem (VRP) solver** built as a **Software-as-a-Service (SaaS)** system. It demonstrates how open-source tools, asynchronous messaging, and distributed architecture can be combined to solve complex computational problems efficiently, without relying on costly commercial solutions.

---

## 🧠 Project Overview

This solution leverages **RabbitMQ**, **Google OR-Tools**, and a **modular, service-based architecture** to solve VRP instances in a scalable and resilient way. The architecture promotes extensibility to other types of computational problems by replacing the solver core.

### Key Highlights:
- ✅ Modular and decoupled service architecture
- ✅ Uses **RabbitMQ** for asynchronous communication (Pub/Sub)
- ✅ Solver uses **Google OR-Tools**
- ✅ Built with scalability and resilience in mind
- ✅ Designed as a **value-for-money** alternative to licensed solvers
- ✅ Academic, non-commercial project focused on demonstrating architectural feasibility

---

## 🧩 System Components

| Component              | Description |
|------------------------|-------------|
| **VRP Data Generator**     | Generates input data for testing various VRP scenarios. |
| **Send Data Service**      | Sends generated data to the message broker. |
| **RabbitMQ**               | Creates and initializes the necessary RabbitMQ components (exchanges, queues, mapping keys etc.) |
| **Broker Web API**         | Publishes data to RabbitMQ queues for processing. |
| **Consume Data Service**   | Subscribes to the appropriate queues, receives data, and synchronizes delivery to the solver. |
| **Solver Web API**         | Receives data on the solver node, processes requests via RESTful endpoints. |
| **Execute VRP Service**    | Creates an instance of the OR-Tools solver and initiates the solving procedure. |
| **OR-Tools Solver**        | Implements the solver logic using [Google OR-Tools](https://developers.google.com/optimization), producing results for each VRP instance. |

---

## 🛠 Technologies Used

- [.NET](https://dotnet.microsoft.com/) (C#)
- [RabbitMQ](https://www.rabbitmq.com/)
- [Google OR-Tools](https://developers.google.com/optimization)
- RESTful Web APIs
- Asynchronous Messaging / Pub-Sub Architecture

---

## 🧪 Scope and Purpose

> This application is **academic** in nature and intended to prove the viability of a SaaS-based approach to solving computationally hard problems (like VRP) using open-source and distributed systems design principles.

While this implementation focuses on **Vehicle Routing Problems**, the system is designed to be flexible. By replacing the core solver logic, other computational problems (e.g., scheduling, optimization, routing) can be plugged into the same framework.

---


### Running the Solution


1. **Clone the repository:**

    ```bash
    git clone https://github.com/yourusername/vrp-solver-saas.git
    cd vrp-solver-saas
    ```

2. **Prerequisites:**

    - [.NET SDK](https://dotnet.microsoft.com/download) (version 8.0)
    - [RabbitMQ](https://www.rabbitmq.com/download.html) installed and running
    - Google OR-Tools library (installed via NuGet or manual setup)

3. **Setup Instructions:**

    - Configure RabbitMQ settings (host, username, password) in the appropriate configuration files (`appsettings.json` or environment variables).
    - Verify that all required NuGet packages are restored (Visual Studio should restore on build).

4. **Build and run the projects:**

    Run the services in the following order depending on your architecture:

    1. VRP Data Generator  
    2. Send Data Service *(currently working for the broker-based architecture)*  
    3. Broker Web API *(only if using the broker-based architecture)*  
    4. Consume Data Service *(only if using the broker-based architecture)*  
    5. Solver Web API *(only if using the broker-based architecture)*  
    6. Execute VRP Service *(only if using direct client-server communication)*  

5. **Troubleshooting:**

    - If you encounter the error `MSB4236: The SDK 'Microsoft.NET.Sdk' specified could not be found`, ensure the .NET SDK is properly installed and your environment variables are set.
    - Make sure RabbitMQ service is running before starting related services.
    - If NuGet packages fail to restore, try running `dotnet restore` in the solution directory.
