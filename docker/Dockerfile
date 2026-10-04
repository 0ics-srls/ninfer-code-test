# Isolated agent workstation for the benchmark: Ubuntu 24.04 with the whole toolchain (.NET 10, Node 24, Google Chrome,
# sqlite3, OpenCode, LSAI) installed by setup/install-ubuntu.sh, as a normal user with sudo. The repository is mounted
# from the host (see compose.yaml), so the agent's work stays on your disk and nothing is installed on your system.
FROM ubuntu:24.04
ARG UID=1000
RUN apt-get update && apt-get install -y --no-install-recommends sudo ca-certificates curl \
 && (userdel -r ubuntu 2>/dev/null || true) \
 && useradd -m -s /bin/bash -u "${UID}" dev \
 && echo "dev ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/dev \
 && rm -rf /var/lib/apt/lists/*
USER dev
WORKDIR /home/dev
COPY --chown=dev:dev setup/install-ubuntu.sh /home/dev/install-ubuntu.sh
RUN TOOLCHAIN_ONLY=1 bash /home/dev/install-ubuntu.sh
ENV DOTNET_ROOT=/home/dev/.dotnet \
    PATH=/home/dev/.dotnet:/home/dev/.dotnet/tools:/home/dev/.opencode/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    OPENCODE_EXPERIMENTAL_OUTPUT_TOKEN_MAX=65536 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /home/dev/ninfer-code-test
CMD ["sleep", "infinity"]
