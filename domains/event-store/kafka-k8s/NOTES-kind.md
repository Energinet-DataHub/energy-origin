# Troubleshooting kind

## inside devcontainer isssue:
```
$ kind create cluster
Creating cluster "kind" ...
 ✓ Ensuring node image (kindest/node:v1.24.0) 🖼 
 ✓ Preparing nodes 📦  
 ✓ Writing configuration 📜 
 ✗ Starting control-plane 🕹️ 
ERROR: failed to create cluster: failed to remove control plane taint: command "docker exec --privileged kind-control-plane kubectl --kubeconfig=/etc/kubernetes/admin.conf taint nodes --all node-role.kubernetes.io/control-plane- node-role.kubernetes.io/master-" failed with error: exit status 1
Command Output: error: stat /etc/kubernetes/admin.conf: no such file or directory
```

## inside devcontainer isssue verbose:
```
$ kind create cluster -v 9999999
Creating cluster "kind" ...
DEBUG: docker/images.go:67] Pulling image: kindest/node:v1.24.0@sha256:0866296e693efe1fed79d5e6c7af8df71fc73ae45e3679af05342239cdc5bc8e ...
 ✓ Ensuring node image (kindest/node:v1.24.0) 🖼 
 ✓ Preparing nodes 📦  
DEBUG: config/config.go:96] Using the following kubeadm config for node kind-control-plane:
apiServer:
  certSANs:
  - localhost
  - 127.0.0.1
  extraArgs:
    runtime-config: ""
apiVersion: kubeadm.k8s.io/v1beta3
clusterName: kind
controlPlaneEndpoint: kind-control-plane:6443
controllerManager:
  extraArgs:
    enable-hostpath-provisioner: "true"
kind: ClusterConfiguration
kubernetesVersion: v1.24.0
networking:
  podSubnet: 10.244.0.0/16
  serviceSubnet: 10.96.0.0/16
scheduler:
  extraArgs: null
---
apiVersion: kubeadm.k8s.io/v1beta3
bootstrapTokens:
- token: abcdef.0123456789abcdef
kind: InitConfiguration
localAPIEndpoint:
  advertiseAddress: 172.18.0.2
  bindPort: 6443
nodeRegistration:
  criSocket: unix:///run/containerd/containerd.sock
  kubeletExtraArgs:
    node-ip: 172.18.0.2
    node-labels: ""
    provider-id: kind://docker/kind/kind-control-plane
---
apiVersion: kubeadm.k8s.io/v1beta3
controlPlane:
  localAPIEndpoint:
    advertiseAddress: 172.18.0.2
    bindPort: 6443
discovery:
  bootstrapToken:
    apiServerEndpoint: kind-control-plane:6443
    token: abcdef.0123456789abcdef
    unsafeSkipCAVerification: true
kind: JoinConfiguration
nodeRegistration:
  criSocket: unix:///run/containerd/containerd.sock
  kubeletExtraArgs:
    node-ip: 172.18.0.2
    node-labels: ""
    provider-id: kind://docker/kind/kind-control-plane
---
apiVersion: kubelet.config.k8s.io/v1beta1
cgroupDriver: systemd
cgroupRoot: /kubelet
evictionHard:
  imagefs.available: 0%
  nodefs.available: 0%
  nodefs.inodesFree: 0%
failSwapOn: false
imageGCHighThresholdPercent: 100
kind: KubeletConfiguration
---
apiVersion: kubeproxy.config.k8s.io/v1alpha1
conntrack:
  maxPerCore: 0
iptables:
  minSyncPeriod: 1s
kind: KubeProxyConfiguration
mode: iptables
 ✓ Writing configuration 📜 
DEBUG: kubeadminit/init.go:82] I0705 10:43:31.095553     127 initconfiguration.go:255] loading configuration from "/kind/kubeadm.conf"
W0705 10:43:31.097175     127 initconfiguration.go:332] [config] WARNING: Ignored YAML document with GroupVersionKind kubeadm.k8s.io/v1beta3, Kind=JoinConfiguration
I0705 10:43:31.104872     127 certs.go:112] creating a new certificate authority for ca
[init] Using Kubernetes version: v1.24.0
[certs] Using certificateDir folder "/etc/kubernetes/pki"
[certs] Generating "ca" certificate and key
I0705 10:43:31.446401     127 certs.go:522] validating certificate period for ca certificate
[certs] Generating "apiserver" certificate and key
[certs] apiserver serving cert is signed for DNS names [kind-control-plane kubernetes kubernetes.default kubernetes.default.svc kubernetes.default.svc.cluster.local localhost] and IPs [10.96.0.1 172.18.0.2 127.0.0.1]
[certs] Generating "apiserver-kubelet-client" certificate and key
I0705 10:43:31.893586     127 certs.go:112] creating a new certificate authority for front-proxy-ca
[certs] Generating "front-proxy-ca" certificate and key
I0705 10:43:32.125305     127 certs.go:522] validating certificate period for front-proxy-ca certificate
[certs] Generating "front-proxy-client" certificate and key
I0705 10:43:32.222852     127 certs.go:112] creating a new certificate authority for etcd-ca
[certs] Generating "etcd/ca" certificate and key
I0705 10:43:32.407556     127 certs.go:522] validating certificate period for etcd/ca certificate
[certs] Generating "etcd/server" certificate and key
[certs] etcd/server serving cert is signed for DNS names [kind-control-plane localhost] and IPs [172.18.0.2 127.0.0.1 ::1]
 ✗ Starting control-plane 🕹️ 
ERROR: failed to create cluster: failed to remove control plane taint: command "docker exec --privileged kind-control-plane kubectl --kubeconfig=/etc/kubernetes/admin.conf taint nodes --all node-role.kubernetes.io/control-plane- node-role.kubernetes.io/master-" failed with error: exit status 1
Command Output: error: stat /etc/kubernetes/admin.conf: no such file or directory
Stack Trace: 
sigs.k8s.io/kind/pkg/errors.WithStack
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/errors/errors.go:59
sigs.k8s.io/kind/pkg/exec.(*LocalCmd).Run
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/exec/local.go:124
sigs.k8s.io/kind/pkg/cluster/internal/providers/docker.(*nodeCmd).Run
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/cluster/internal/providers/docker/node.go:146
sigs.k8s.io/kind/pkg/cluster/internal/create/actions/kubeadminit.(*action).Execute
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/cluster/internal/create/actions/kubeadminit/init.go:140
sigs.k8s.io/kind/pkg/cluster/internal/create.Cluster
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/cluster/internal/create/create.go:135
sigs.k8s.io/kind/pkg/cluster.(*Provider).Create
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/cluster/provider.go:182
sigs.k8s.io/kind/pkg/cmd/kind/create/cluster.runE
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/cmd/kind/create/cluster/createcluster.go:80
sigs.k8s.io/kind/pkg/cmd/kind/create/cluster.NewCommand.func1
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/pkg/cmd/kind/create/cluster/createcluster.go:55
github.com/spf13/cobra.(*Command).execute
        /go/pkg/mod/github.com/spf13/cobra@v1.4.0/command.go:856
github.com/spf13/cobra.(*Command).ExecuteC
        /go/pkg/mod/github.com/spf13/cobra@v1.4.0/command.go:974
github.com/spf13/cobra.(*Command).Execute
        /go/pkg/mod/github.com/spf13/cobra@v1.4.0/command.go:902
sigs.k8s.io/kind/cmd/kind/app.Run
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/cmd/kind/app/main.go:53
sigs.k8s.io/kind/cmd/kind/app.Main
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/cmd/kind/app/main.go:35
main.main
        /go/pkg/mod/sigs.k8s.io/kind@v0.14.0/main.go:25
runtime.main
        /usr/local/go/src/runtime/proc.go:250
runtime.goexit
        /usr/local/go/src/runtime/asm_amd64.s:1571
```

## outside devcontainer:
```
% kind create cluster -v 9999
Creating cluster "kind" ...
DEBUG: docker/images.go:67] Pulling image: kindest/node:v1.24.0@sha256:0866296e693efe1fed79d5e6c7af8df71fc73ae45e3679af05342239cdc5bc8e ...
 ✓ Ensuring node image (kindest/node:v1.24.0) 🖼 
 ✓ Preparing nodes 📦  
DEBUG: config/config.go:96] Using the following kubeadm config for node kind-control-plane:
apiServer:
  certSANs:
  - localhost
  - 127.0.0.1
  extraArgs:
    runtime-config: ""
apiVersion: kubeadm.k8s.io/v1beta3
clusterName: kind
controlPlaneEndpoint: kind-control-plane:6443
controllerManager:
  extraArgs:
    enable-hostpath-provisioner: "true"
kind: ClusterConfiguration
kubernetesVersion: v1.24.0
networking:
  podSubnet: 10.244.0.0/16
  serviceSubnet: 10.96.0.0/16
scheduler:
  extraArgs: null
---
apiVersion: kubeadm.k8s.io/v1beta3
bootstrapTokens:
- token: abcdef.0123456789abcdef
kind: InitConfiguration
localAPIEndpoint:
  advertiseAddress: 172.18.0.2
  bindPort: 6443
nodeRegistration:
  criSocket: unix:///run/containerd/containerd.sock
  kubeletExtraArgs:
    node-ip: 172.18.0.2
    node-labels: ""
    provider-id: kind://docker/kind/kind-control-plane
---
apiVersion: kubeadm.k8s.io/v1beta3
controlPlane:
  localAPIEndpoint:
    advertiseAddress: 172.18.0.2
    bindPort: 6443
discovery:
  bootstrapToken:
    apiServerEndpoint: kind-control-plane:6443
    token: abcdef.0123456789abcdef
    unsafeSkipCAVerification: true
kind: JoinConfiguration
nodeRegistration:
  criSocket: unix:///run/containerd/containerd.sock
  kubeletExtraArgs:
    node-ip: 172.18.0.2
    node-labels: ""
    provider-id: kind://docker/kind/kind-control-plane
---
apiVersion: kubelet.config.k8s.io/v1beta1
cgroupDriver: systemd
cgroupRoot: /kubelet
evictionHard:
  imagefs.available: 0%
  nodefs.available: 0%
  nodefs.inodesFree: 0%
failSwapOn: false
imageGCHighThresholdPercent: 100
kind: KubeletConfiguration
---
apiVersion: kubeproxy.config.k8s.io/v1alpha1
conntrack:
  maxPerCore: 0
iptables:
  minSyncPeriod: 1s
kind: KubeProxyConfiguration
mode: iptables
 ✓ Writing configuration 📜 
DEBUG: kubeadminit/init.go:82] I0705 14:11:13.096888     128 initconfiguration.go:255] loading configuration from "/kind/kubeadm.conf"
W0705 14:11:13.097866     128 initconfiguration.go:332] [config] WARNING: Ignored YAML document with GroupVersionKind kubeadm.k8s.io/v1beta3, Kind=JoinConfiguration
[init] Using Kubernetes version: v1.24.0
[certs] Using certificateDir folder "/etc/kubernetes/pki"
I0705 14:11:13.105480     128 certs.go:112] creating a new certificate authority for ca
[certs] Generating "ca" certificate and key
I0705 14:11:13.336289     128 certs.go:522] validating certificate period for ca certificate
[certs] Generating "apiserver" certificate and key
[certs] apiserver serving cert is signed for DNS names [kind-control-plane kubernetes kubernetes.default kubernetes.default.svc kubernetes.default.svc.cluster.local localhost] and IPs [10.96.0.1 172.18.0.2 127.0.0.1]
[certs] Generating "apiserver-kubelet-client" certificate and key
I0705 14:11:14.008862     128 certs.go:112] creating a new certificate authority for front-proxy-ca
[certs] Generating "front-proxy-ca" certificate and key
I0705 14:11:14.397715     128 certs.go:522] validating certificate period for front-proxy-ca certificate
[certs] Generating "front-proxy-client" certificate and key
I0705 14:11:14.463820     128 certs.go:112] creating a new certificate authority for etcd-ca
[certs] Generating "etcd/ca" certificate and key
I0705 14:11:14.678944     128 certs.go:522] validating certificate period for etcd/ca certificate
[certs] Generating "etcd/server" certificate and key
[certs] etcd/server serving cert is signed for DNS names [kind-control-plane localhost] and IPs [172.18.0.2 127.0.0.1 ::1]
[certs] Generating "etcd/peer" certificate and key
[certs] etcd/peer serving cert is signed for DNS names [kind-control-plane localhost] and IPs [172.18.0.2 127.0.0.1 ::1]
[certs] Generating "etcd/healthcheck-client" certificate and key
[certs] Generating "apiserver-etcd-client" certificate and key
I0705 14:11:15.992011     128 certs.go:78] creating new public/private key files for signing service account users
[certs] Generating "sa" key and public key
I0705 14:11:16.112660     128 kubeconfig.go:103] creating kubeconfig file for admin.conf
[kubeconfig] Using kubeconfig folder "/etc/kubernetes"
[kubeconfig] Writing "admin.conf" kubeconfig file
I0705 14:11:16.336609     128 kubeconfig.go:103] creating kubeconfig file for kubelet.conf
[kubeconfig] Writing "kubelet.conf" kubeconfig file
I0705 14:11:16.454295     128 kubeconfig.go:103] creating kubeconfig file for controller-manager.conf
[kubeconfig] Writing "controller-manager.conf" kubeconfig file
I0705 14:11:16.742521     128 kubeconfig.go:103] creating kubeconfig file for scheduler.conf
[kubeconfig] Writing "scheduler.conf" kubeconfig file
I0705 14:11:17.050563     128 kubelet.go:65] Stopping the kubelet
[kubelet-start] Writing kubelet environment file with flags to file "/var/lib/kubelet/kubeadm-flags.env"
[kubelet-start] Writing kubelet configuration to file "/var/lib/kubelet/config.yaml"
[kubelet-start] Starting the kubelet
I0705 14:11:17.206769     128 manifests.go:99] [control-plane] getting StaticPodSpecs
[control-plane] Using manifest folder "/etc/kubernetes/manifests"
[control-plane] Creating static Pod manifest for "kube-apiserver"
I0705 14:11:17.207057     128 certs.go:522] validating certificate period for CA certificate
I0705 14:11:17.207207     128 manifests.go:125] [control-plane] adding volume "ca-certs" for component "kube-apiserver"
I0705 14:11:17.207273     128 manifests.go:125] [control-plane] adding volume "etc-ca-certificates" for component "kube-apiserver"
I0705 14:11:17.207282     128 manifests.go:125] [control-plane] adding volume "k8s-certs" for component "kube-apiserver"
I0705 14:11:17.207340     128 manifests.go:125] [control-plane] adding volume "usr-local-share-ca-certificates" for component "kube-apiserver"
I0705 14:11:17.207369     128 manifests.go:125] [control-plane] adding volume "usr-share-ca-certificates" for component "kube-apiserver"
I0705 14:11:17.209428     128 manifests.go:154] [control-plane] wrote static Pod manifest for component "kube-apiserver" to "/etc/kubernetes/manifests/kube-apiserver.yaml"
I0705 14:11:17.209464     128 manifests.go:99] [control-plane] getting StaticPodSpecs
[control-plane] Creating static Pod manifest for "kube-controller-manager"
I0705 14:11:17.210034     128 manifests.go:125] [control-plane] adding volume "ca-certs" for component "kube-controller-manager"
I0705 14:11:17.210071     128 manifests.go:125] [control-plane] adding volume "etc-ca-certificates" for component "kube-controller-manager"
I0705 14:11:17.210132     128 manifests.go:125] [control-plane] adding volume "flexvolume-dir" for component "kube-controller-manager"
I0705 14:11:17.210140     128 manifests.go:125] [control-plane] adding volume "k8s-certs" for component "kube-controller-manager"
I0705 14:11:17.210156     128 manifests.go:125] [control-plane] adding volume "kubeconfig" for component "kube-controller-manager"
I0705 14:11:17.210165     128 manifests.go:125] [control-plane] adding volume "usr-local-share-ca-certificates" for component "kube-controller-manager"
I0705 14:11:17.210172     128 manifests.go:125] [control-plane] adding volume "usr-share-ca-certificates" for component "kube-controller-manager"
[control-plane] Creating static Pod manifest for "kube-scheduler"
I0705 14:11:17.211838     128 manifests.go:154] [control-plane] wrote static Pod manifest for component "kube-controller-manager" to "/etc/kubernetes/manifests/kube-controller-manager.yaml"
I0705 14:11:17.211891     128 manifests.go:99] [control-plane] getting StaticPodSpecs
I0705 14:11:17.212434     128 manifests.go:125] [control-plane] adding volume "kubeconfig" for component "kube-scheduler"
I0705 14:11:17.213634     128 manifests.go:154] [control-plane] wrote static Pod manifest for component "kube-scheduler" to "/etc/kubernetes/manifests/kube-scheduler.yaml"
[etcd] Creating static Pod manifest for local etcd in "/etc/kubernetes/manifests"
I0705 14:11:17.215951     128 local.go:65] [etcd] wrote Static Pod manifest for a local etcd member to "/etc/kubernetes/manifests/etcd.yaml"
I0705 14:11:17.215996     128 waitcontrolplane.go:83] [wait-control-plane] Waiting for the API server to be healthy
I0705 14:11:17.217172     128 loader.go:372] Config loaded from file:  /etc/kubernetes/admin.conf
[wait-control-plane] Waiting for the kubelet to boot up the control plane as static Pods from directory "/etc/kubernetes/manifests". This can take up to 4m0s
I0705 14:11:17.219298     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 0 milliseconds
I0705 14:11:17.722254     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 2 milliseconds
I0705 14:11:18.222216     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 2 milliseconds
I0705 14:11:18.721726     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 1 milliseconds
I0705 14:11:19.220081     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 0 milliseconds
I0705 14:11:19.720211     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 0 milliseconds
I0705 14:11:20.221327     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 0 milliseconds
I0705 14:11:20.720616     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s  in 0 milliseconds
I0705 14:11:26.822995     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s 500 Internal Server Error in 5624 milliseconds
I0705 14:11:27.202601     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s 500 Internal Server Error in 4 milliseconds
I0705 14:11:27.701335     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s 500 Internal Server Error in 2 milliseconds
I0705 14:11:28.199680     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s 500 Internal Server Error in 1 milliseconds
I0705 14:11:28.700423     128 round_trippers.go:553] GET https://kind-control-plane:6443/healthz?timeout=10s 200 OK in 2 milliseconds
[apiclient] All control plane components are healthy after 11.504744 seconds
I0705 14:11:28.700676     128 uploadconfig.go:110] [upload-config] Uploading the kubeadm ClusterConfiguration to a ConfigMap
[upload-config] Storing the configuration used in ConfigMap "kubeadm-config" in the "kube-system" Namespace
I0705 14:11:28.704316     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/configmaps?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:28.707935     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/roles?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:28.711262     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/rolebindings?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:28.711505     128 uploadconfig.go:124] [upload-config] Uploading the kubelet component config to a ConfigMap
[kubelet] Creating a ConfigMap "kubelet-config" in namespace kube-system with the configuration for the kubelets in the cluster
I0705 14:11:28.714465     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/configmaps?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:28.717020     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/roles?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:28.719756     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/rolebindings?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:28.719933     128 uploadconfig.go:129] [upload-config] Preserving the CRISocket information for the control-plane node
I0705 14:11:28.719981     128 patchnode.go:31] [patchnode] Uploading the CRI Socket information "unix:///run/containerd/containerd.sock" to the Node API object "kind-control-plane" as an annotation
I0705 14:11:29.226523     128 round_trippers.go:553] GET https://kind-control-plane:6443/api/v1/nodes/kind-control-plane?timeout=10s 200 OK in 5 milliseconds
I0705 14:11:29.236128     128 round_trippers.go:553] PATCH https://kind-control-plane:6443/api/v1/nodes/kind-control-plane?timeout=10s 200 OK in 6 milliseconds
[upload-certs] Skipping phase. Please see --upload-certs
[mark-control-plane] Marking the node kind-control-plane as control-plane by adding the labels: [node-role.kubernetes.io/control-plane node.kubernetes.io/exclude-from-external-load-balancers]
[mark-control-plane] Marking the node kind-control-plane as control-plane by adding the taints [node-role.kubernetes.io/master:NoSchedule node-role.kubernetes.io/control-plane:NoSchedule]
I0705 14:11:29.743973     128 round_trippers.go:553] GET https://kind-control-plane:6443/api/v1/nodes/kind-control-plane?timeout=10s 200 OK in 5 milliseconds
I0705 14:11:29.751640     128 round_trippers.go:553] PATCH https://kind-control-plane:6443/api/v1/nodes/kind-control-plane?timeout=10s 200 OK in 4 milliseconds
[bootstrap-token] Configuring bootstrap tokens, cluster-info ConfigMap, RBAC Roles
I0705 14:11:29.756404     128 round_trippers.go:553] GET https://kind-control-plane:6443/api/v1/namespaces/kube-system/secrets/bootstrap-token-abcdef?timeout=10s 404 Not Found in 3 milliseconds
I0705 14:11:29.760307     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/secrets?timeout=10s 201 Created in 3 milliseconds
[bootstrap-token] Configured RBAC rules to allow Node Bootstrap tokens to get nodes
I0705 14:11:29.764139     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterroles?timeout=10s 201 Created in 3 milliseconds
I0705 14:11:29.767116     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterrolebindings?timeout=10s 201 Created in 2 milliseconds
[bootstrap-token] Configured RBAC rules to allow Node Bootstrap tokens to post CSRs in order for nodes to get long term certificate credentials
[bootstrap-token] Configured RBAC rules to allow the csrapprover controller automatically approve CSRs from a Node Bootstrap Token
I0705 14:11:29.770776     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterrolebindings?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.773493     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterrolebindings?timeout=10s 201 Created in 2 milliseconds
[bootstrap-token] Configured RBAC rules to allow certificate rotation for all node client certificates in the cluster
I0705 14:11:29.776289     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterrolebindings?timeout=10s 201 Created in 2 milliseconds
[bootstrap-token] Creating the "cluster-info" ConfigMap in the "kube-public" namespace
I0705 14:11:29.776443     128 clusterinfo.go:47] [bootstrap-token] loading admin kubeconfig
I0705 14:11:29.777053     128 loader.go:372] Config loaded from file:  /etc/kubernetes/admin.conf
I0705 14:11:29.777087     128 clusterinfo.go:58] [bootstrap-token] copying the cluster from admin.conf to the bootstrap kubeconfig
I0705 14:11:29.778966     128 clusterinfo.go:70] [bootstrap-token] creating/updating ConfigMap in kube-public namespace
I0705 14:11:29.781747     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-public/configmaps?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.782213     128 clusterinfo.go:84] creating the RBAC rules for exposing the cluster-info ConfigMap in the kube-public namespace
I0705 14:11:29.784686     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-public/roles?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.787993     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-public/rolebindings?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.788330     128 kubeletfinalize.go:90] [kubelet-finalize] Assuming that kubelet client certificate rotation is enabled: found "/var/lib/kubelet/pki/kubelet-client-current.pem"
[kubelet-finalize] Updating "/etc/kubernetes/kubelet.conf" to point to a rotatable kubelet client certificate and key
I0705 14:11:29.788874     128 loader.go:372] Config loaded from file:  /etc/kubernetes/kubelet.conf
I0705 14:11:29.789379     128 kubeletfinalize.go:134] [kubelet-finalize] Restarting the kubelet to enable client certificate rotation
I0705 14:11:29.974098     128 round_trippers.go:553] GET https://kind-control-plane:6443/apis/apps/v1/namespaces/kube-system/deployments?labelSelector=k8s-app%3Dkube-dns 200 OK in 3 milliseconds
I0705 14:11:29.977616     128 round_trippers.go:553] GET https://kind-control-plane:6443/api/v1/namespaces/kube-system/configmaps/coredns?timeout=10s 404 Not Found in 1 milliseconds
I0705 14:11:29.980583     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/configmaps?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.983821     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterroles?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.986857     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterrolebindings?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:29.990341     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/serviceaccounts?timeout=10s 201 Created in 3 milliseconds
I0705 14:11:29.995996     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/apps/v1/namespaces/kube-system/deployments?timeout=10s 201 Created in 4 milliseconds
I0705 14:11:30.002301     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/services?timeout=10s 201 Created in 4 milliseconds
[addons] Applied essential addon: CoreDNS
I0705 14:11:30.005535     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/serviceaccounts?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:30.008968     128 round_trippers.go:553] POST https://kind-control-plane:6443/api/v1/namespaces/kube-system/configmaps?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:30.014256     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/apps/v1/namespaces/kube-system/daemonsets?timeout=10s 201 Created in 4 milliseconds
I0705 14:11:30.016859     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/clusterrolebindings?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:30.020131     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/roles?timeout=10s 201 Created in 2 milliseconds
I0705 14:11:30.162425     128 request.go:533] Waited for 141.95213ms due to client-side throttling, not priority and fairness, request: POST:https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/rolebindings?timeout=10s
I0705 14:11:30.168323     128 round_trippers.go:553] POST https://kind-control-plane:6443/apis/rbac.authorization.k8s.io/v1/namespaces/kube-system/rolebindings?timeout=10s 201 Created in 5 milliseconds
[addons] Applied essential addon: kube-proxy
I0705 14:11:30.168977     128 loader.go:372] Config loaded from file:  /etc/kubernetes/admin.conf
I0705 14:11:30.169587     128 loader.go:372] Config loaded from file:  /etc/kubernetes/admin.conf

Your Kubernetes control-plane has initialized successfully!

To start using your cluster, you need to run the following as a regular user:

  mkdir -p $HOME/.kube
  sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
  sudo chown $(id -u):$(id -g) $HOME/.kube/config

Alternatively, if you are the root user, you can run:

  export KUBECONFIG=/etc/kubernetes/admin.conf

You should now deploy a pod network to the cluster.
Run "kubectl apply -f [podnetwork].yaml" with one of the options listed at:
  https://kubernetes.io/docs/concepts/cluster-administration/addons/

You can now join any number of control-plane nodes by copying certificate authorities
and service account keys on each node and then running the following as root:

  kubeadm join kind-control-plane:6443 --token <value withheld> \
	--discovery-token-ca-cert-hash sha256:8f7d3f26446d8f74c26d908f370f2f44c76e81e7d1784ecd1d75dd6da4b79bbe \
	--control-plane 

Then you can join any number of worker nodes by running the following on each as root:

kubeadm join kind-control-plane:6443 --token <value withheld> \
	--discovery-token-ca-cert-hash sha256:8f7d3f26446d8f74c26d908f370f2f44c76e81e7d1784ecd1d75dd6da4b79bbe 
 ✓ Starting control-plane 🕹️ 
DEBUG: installcni/cni.go:120] Using the following Kindnetd config:
null
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: kindnet
rules:
- apiGroups:
  - policy
  resourceNames:
  - kindnet
  resources:
  - podsecuritypolicies
  verbs:
  - use
- apiGroups:
  - ""
  resources:
  - nodes
  verbs:
  - list
  - watch
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: kindnet
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: kindnet
subjects:
- kind: ServiceAccount
  name: kindnet
  namespace: kube-system
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: kindnet
  namespace: kube-system
---
apiVersion: apps/v1
kind: DaemonSet
metadata:
  labels:
    app: kindnet
    k8s-app: kindnet
    tier: node
  name: kindnet
  namespace: kube-system
spec:
  selector:
    matchLabels:
      app: kindnet
  template:
    metadata:
      labels:
        app: kindnet
        k8s-app: kindnet
        tier: node
    spec:
      containers:
      - env:
        - name: HOST_IP
          valueFrom:
            fieldRef:
              fieldPath: status.hostIP
        - name: POD_IP
          valueFrom:
            fieldRef:
              fieldPath: status.podIP
        - name: POD_SUBNET
          value: 10.244.0.0/16
        - name: CONTROL_PLANE_ENDPOINT
          value: kind-control-plane:6443
        image: docker.io/kindest/kindnetd:v20220510-4929dd75
        name: kindnet-cni
        resources:
          limits:
            cpu: 100m
            memory: 50Mi
          requests:
            cpu: 100m
            memory: 50Mi
        securityContext:
          capabilities:
            add:
            - NET_RAW
            - NET_ADMIN
          privileged: false
        volumeMounts:
        - mountPath: /etc/cni/net.d
          name: cni-cfg
        - mountPath: /run/xtables.lock
          name: xtables-lock
          readOnly: false
        - mountPath: /lib/modules
          name: lib-modules
          readOnly: true
      hostNetwork: true
      serviceAccountName: kindnet
      tolerations:
      - operator: Exists
      volumes:
      - hostPath:
          path: /etc/cni/net.d
        name: cni-cfg
      - hostPath:
          path: /run/xtables.lock
          type: FileOrCreate
        name: xtables-lock
      - hostPath:
          path: /lib/modules
        name: lib-modules
 ✓ Installing CNI 🔌 
 ✓ Installing StorageClass 💾 
Set kubectl context to "kind-kind"
You can now use your cluster with:

kubectl cluster-info --context kind-kind

Have a nice day! 👋
```