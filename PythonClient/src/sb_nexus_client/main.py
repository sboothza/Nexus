import grpc
from src.sb_nexus_client.grpc import service_pb2

from src.sb_nexus_client.grpc.service_pb2_grpc import CallerStub


def main():
    qr = service_pb2.QueryRequest()
    qr.bindingName = "bindingname"
    qr.extraInfo["topic"] = "extra"
    qr.data = "data"

    print(qr)

    channel = grpc.insecure_channel("localhost:50051")
    stub = CallerStub(channel)
    response = stub.Query(qr)

    print(response)


if __name__ == '__main__':
    main()
