import socket

UDP_IP = "127.0.0.1"
UDP_PORT = 5005

sock = socket.socket(socket.AF_INET,  # Internet
                     socket.SOCK_DGRAM)  # UDP
sock.bind((UDP_IP, UDP_PORT))

count = 0

while True:
    data, addr = sock.recvfrom(16384)

    count += 1

    if len(data) == 12:
        print("lkdajfldks")
        count = 0
    # print("received message: %s" % data)
