# Gateway.API

## Role
This is the API Gateway for the Video Streaming Platform, built on top of Microsoft's **YARP** (Yet Another Reverse Proxy) library.

## Impact on Solution
It acts as the unified frontend ingress edge for all frontend traffic.
- All requests entering the platform hit this node first.
- It routes `/api/auth/*` requests to the Identity service.
- It routes `/api/videos/*` and `/api/upload/*` requests to the VideoManagement service.
- It routes `/api/stream/*` requests to the Streaming service.
By consolidating this on the Gateway, we maintain a single cohesive API plane without exposing backend microservice IPs or varying ports to the public frontend applications.
