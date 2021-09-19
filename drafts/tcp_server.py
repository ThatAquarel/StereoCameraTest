import socket
import struct

import numpy as np
import cv2


class ServerProtocol:

    def __init__(self):
        self.socket = None

        self.dt = np.dtype(np.ubyte)
        self.dt = self.dt.newbyteorder(">")

    def listen(self, server_ip, server_port):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.bind((server_ip, server_port))
        self.socket.listen(1)

    def handle_images(self):
        connection, address = self.socket.accept()

        while True:
            data = connection.recv(12)
            header = struct.unpack(">ccicci", data)

            length = header[2] * header[5] * 3

            data = b''
            while len(data) < length:
                remaining = length - len(data)
                data += connection.recv(
                    4096 if remaining > 4096 else remaining)

            frame = np.frombuffer(data, dtype=self.dt)
            frame = np.reshape(frame, (header[2], header[5], 3), "F")
            frame = np.rot90(frame)

            cv2.imshow('frame', frame[:, :, (2, 1, 0)])

            if cv2.waitKey(1) == ord('q'):
                break

        cv2.destroyAllWindows()

    def close(self):
        self.socket.close()
        self.socket = None


if __name__ == '__main__':
    sp = ServerProtocol()
    sp.listen('127.0.0.1', 5005)
    sp.handle_images()
