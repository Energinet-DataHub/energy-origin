# NOTES

enable k8s in docker
get kind installed

```
kind create cluster
kubectl create namespace kafka
kubectl create -f 'https://strimzi.io/install/latest?namespace=kafka' -n kafka # a lot of things

kubectl get pod -n kafka --watch # wait for it to be up

kubectl apply -f https://strimzi.io/examples/latest/kafka/kafka-persistent-single.yaml -n kafka

kubectl wait kafka/a-cluster --for=condition=Ready --timeout=300s -n kafka # wait for it to come up



kubectl apply -f kafka-custom.yaml -n kafka
kubectl describe -n kafka kafkas # bootstrap
```

```
kubectl -n kafka run kafka-producer -ti --image=quay.io/strimzi/kafka:0.29.0-kafka-3.2.0 --rm=true --restart=Never -- bin/kafka-console-producer.sh --bootstrap-server a-cluster-kafka-bootstrap:9092 --topic my-topic
kubectl -n kafka run kafka-consumer -ti --image=quay.io/strimzi/kafka:0.29.0-kafka-3.2.0 --rm=true --restart=Never -- bin/kafka-console-consumer.sh --bootstrap-server a-cluster-kafka-bootstrap:9092 --topic my-topic --from-beginning
```

https://github.com/confluentinc/confluent-kafka-dotnet

https://portworx.com/blog/choosing-the-right-kubernetes-operator-for-apache-kafka/

https://strimzi.io/blog/2019/11/05/exposing-http-bridge/



https://redis.io/docs/about/ - Must be (all) in-memory
https://www.eventstore.com/blog/event-store-on-kubernetes - No operator, heard others fail, no big case studies nor mention of performance in them
https://kafka.apache.org/ - a choice of operators, battletested, open source, heard many succeed, testimonals like:
> Kafka is powering our high-flow event pipeline that aggregates over 1.2 billion metric series from 1000+ data centers for near-to-real time data center operational analytics and modeling


open tabs:
https://blog.devgenius.io/apache-kafka-on-kubernetes-using-strimzi-27d47b6b13bc
https://developer.confluent.io/get-started/dotnet/#consume-events


Questions:
- how to configure within azure

kind: StorageClass -> allowVolumeExpansion: true


```
# kubectl apply -f kafka-bridge.yaml -n kafka
# kubectl expose svc my-bridge-bridge-service -n kafka --port=8080 --target-port=8080
# kubectl port-forward -n kafka svc/my-bridge-bridge-service 8080:8080

-> https://kind.sigs.k8s.io/docs/user/configuration/
```






ceph/rook - maximum file count and storage





publish
 - event (json)
 - tags/topics?

subscribe(start from: Date?) (vector-clock?)
 - on topics
 - or tag filter

conflict handling

model instanticing based on version







