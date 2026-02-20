"""TCP client for sending BTXML scripts to BarTender Commander."""

import socket
import logging
from typing import Optional

from .models import BarTenderConfig

logger = logging.getLogger(__name__)


class CommanderClient:
    """Client for communicating with BarTender Commander via TCP/IP socket.

    BarTender Commander listens on a configurable TCP port (default 5170)
    and accepts BTXML scripts. It returns an XML response with print job status.
    """

    def __init__(self, config: BarTenderConfig):
        self.config = config
        self._socket: Optional[socket.socket] = None

    def send(self, btxml: str) -> str:
        """Send a BTXML script to BarTender Commander and return the response.

        Args:
            btxml: Complete BTXML XML string.

        Returns:
            XML response string from BarTender.

        Raises:
            ConnectionError: If unable to connect to Commander.
            TimeoutError: If the connection or response times out.
        """
        encoded = btxml.encode("utf-8")
        logger.info(
            "Sending BTXML to %s:%d (%d bytes)",
            self.config.host, self.config.port, len(encoded),
        )

        try:
            self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self._socket.settimeout(self.config.timeout)
            self._socket.connect((self.config.host, self.config.port))
            logger.debug("Connected to Commander")

            self._socket.sendall(encoded)
            logger.debug("BTXML sent, waiting for response...")

            response = self._receive_response()
            logger.info("Received response (%d bytes)", len(response))
            return response

        except socket.timeout as e:
            raise TimeoutError(
                f"Timeout communicating with Commander at "
                f"{self.config.host}:{self.config.port}"
            ) from e
        except OSError as e:
            raise ConnectionError(
                f"Cannot connect to Commander at "
                f"{self.config.host}:{self.config.port}: {e}"
            ) from e
        finally:
            self.close()

    def _receive_response(self) -> str:
        """Read the full response from Commander."""
        chunks: list[bytes] = []
        while True:
            try:
                chunk = self._socket.recv(4096)
                if not chunk:
                    break
                chunks.append(chunk)
            except socket.timeout:
                # If we already received data, treat timeout as end of response
                if chunks:
                    break
                raise
        return b"".join(chunks).decode("utf-8", errors="replace")

    def close(self) -> None:
        """Close the TCP socket."""
        if self._socket:
            try:
                self._socket.close()
            except OSError:
                pass
            self._socket = None

    def test_connection(self) -> bool:
        """Test if Commander is reachable.

        Returns:
            True if a TCP connection can be established, False otherwise.
        """
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(5.0)
            sock.connect((self.config.host, self.config.port))
            sock.close()
            logger.info("Commander is reachable at %s:%d", self.config.host, self.config.port)
            return True
        except OSError as e:
            logger.warning(
                "Commander not reachable at %s:%d: %s",
                self.config.host, self.config.port, e,
            )
            return False
