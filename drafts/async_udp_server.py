import struct
import asyncio
import numpy as np
from asyncio import BaseProtocol


# noinspection PyMethodMayBeStatic
class CameraFrameGenerator(BaseProtocol):
    def __init__(self):
        self.transport = None
        self.pixels = None
        self.offset = 0

        self.dt = np.dtype(np.ubyte)
        self.dt = self.dt.newbyteorder(">")

        self.packet_count = 0

    def connection_made(self, transport):
        self.transport = transport

    def datagram_received(self, data, _):
        # header = struct.unpack(">ccicci", data[0:12])
        # frame_view = np.frombuffer(data, dtype=self.dt)
        #
        # if header[0:2] == (b'w', b'=') and header[3:5] == (b'h', b'='):
        #     bytes_length = header[2] * header[5] * 3
        #     self.pixels = np.empty(bytes_length, dtype=self.dt)
        #     self.offset = 0
        #
        #     frame_view = frame_view[12:]
        #
        # self.pixels[self.offset:self.offset + frame_view.shape[0]] = frame_view
        # self.offset += frame_view.shape[0]
        #
        # if len(data) == 12:
        #     print("dlskafjdslk")

        if len(data) == 12:
            header = struct.unpack(">ccicci", data)
            bytes_length = header[2] * header[5] * 3
            self.pixels = np.empty(bytes_length, dtype=self.dt)
            self.offset = 0

            self.packet_count = 0
        else:
            frame_view = np.frombuffer(data, dtype=self.dt)
            self.pixels[self.offset: self.offset + frame_view.shape[0]] = frame_view
            self.offset += frame_view.shape[0]

            self.packet_count += 1


async def main():
    loop = asyncio.get_running_loop()
    transport, protocol = await loop.create_datagram_endpoint(
        lambda: CameraFrameGenerator(),
        local_addr=('127.0.0.1', 5005))

    try:
        while True:
            await asyncio.sleep(1)
    finally:
        transport.close()


if __name__ == '__main__':
    asyncio.run(main())
