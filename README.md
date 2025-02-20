# Unity P2P Networking

## Overview
This project provides a networking solution for Unity applications, specifically focused on peer-to-peer (P2P) communication using TCP and UDP protocols. It includes functionality for networked game object instantiation, synchronization, method broadcasting, and handling network failures.

## Features
- Network Object Management: Supports instantiating, destroying, and managing networked game objects across all clients.
- Method Broadcasting: Allows broadcasting methods across clients using custom payloads.
- Client Management: Identifies and manages clients in a peer-to-peer network.
- Protocol Support: Supports both TCP and UDP for different types of network communication.
- Custom Serialization: Custom serialization and deserialization of network data, ensuring compatibility across different game objects.

## Getting Started

### Setup
1. Add the src code to Your Project: Import the NetMaster source code and its dependencies into your Unity project.
Configure Settings:
2. In the Unity Editor, attach the NetMaster script to a GameObject.
3. Configure the NetSettingsData in the Inspector to adjust network parameters such as the maximum number of net game objects.
4. Inherit from NetMonoBehaviour in order to broadcast methods to other players.
5. Instantiate Networked Game Objects: Use the InstantiateNetPrefab() method to create game objects that will be able to synchronize across the network.
6. In order to connect players together via a lobby you will need to create your own implementation of a lobby script using NetMaster.Service directly.

### Installation
Clone the repository:
```sh
git clone https://github.com/abde-bout/UnityP2PNet.git
```

#### Usage Example
Checkout the .cs file "SyncTransform" to understand how a NetMonoBehaviour can sync to other players.

##Dependencies
- HostileGrounds library
- P2PNet library

## License
This project is licensed under the MIT License.

## Contact
For questions or issues, open an issue on GitHub.

