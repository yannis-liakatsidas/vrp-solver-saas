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
| **Broker Web API**         | Publishes data to RabbitMQ queues for processing. |
| **Consume Data Service**   | Subscribes to the appropriate queues, receives data, and synchronizes delivery to the solver. |
| **Solver Web API**         | Receives data on the solver node, processes requests via RESTful endpoints. |
| **Execute VRP Service**    | Implements the solver logic using [Google OR-Tools](https://developers.google.com/optimization), producing results for each VRP instance. |

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

## 🚀 Getting Started

### Prerequisites

- .NET 6 or higher
- RabbitMQ (local or hosted)
- Visual Studio 2022+
- Internet access (for restoring NuGet packages)

### Running the Solution

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/vrp-solver-saas.git
   cd vrp-solver-saas
### How to push your Visual Studio solution to the new GitHub repo

1. **Clone the empty repo locally** (optional but recommended):


    ```bash
    git clone https://github.com/yourusername/your-repo.git
    cd your-repo
    ```

2. **Copy your Visual Studio solution files into this folder**

    - Copy your `.sln` file, project folders, source code, `.gitignore` (if you have one), and all other relevant files into this cloned folder.

3. **Check or add `.gitignore`**

    - Make sure you have a `.gitignore` for Visual Studio projects to avoid committing unnecessary files.
    - If you don’t have one, create it or download a standard Visual Studio `.gitignore`.

4. **Add all files to Git**

    
    ```bash
    git add .
    ```

5. **Commit your changes**

    
    ```bash
    git commit -m "Initial commit: add Visual Studio solution and projects"
    ```

6. **Push your commit to GitHub**

    If your local branch is named `master` but your remote expects `main`, rename your branch first:

    
    ```bash
    git branch -M main
    ```

    Then push:

    
    ```bash
    git push -u origin main
    ```

7. **Verify**

    - Go to your GitHub repository in your browser.
    - Refresh the page to see all your files uploaded.

