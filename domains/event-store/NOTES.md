
enable k8s in docker

get kind installed

```
kind create cluster
kubectl create namespace kafka
kubectl create -f 'https://strimzi.io/install/latest?namespace=kafka' -n kafka # a lot of things

kubectl get pod -n kafka --watch # wait for it to be up

kubectl apply -f https://strimzi.io/examples/latest/kafka/kafka-persistent-single.yaml -n kafka

kubectl wait kafka/my-cluster --for=condition=Ready --timeout=300s -n kafka # wait for it to come up
```

```
kubectl -n kafka run kafka-producer -ti --image=quay.io/strimzi/kafka:0.29.0-kafka-3.2.0 --rm=true --restart=Never -- bin/kafka-console-producer.sh --bootstrap-server my-cluster-kafka-bootstrap:9092 --topic my-topic
kubectl -n kafka run kafka-consumer -ti --image=quay.io/strimzi/kafka:0.29.0-kafka-3.2.0 --rm=true --restart=Never -- bin/kafka-console-consumer.sh --bootstrap-server my-cluster-kafka-bootstrap:9092 --topic my-topic --from-beginning
```

https://github.com/confluentinc/confluent-kafka-dotnet

https://portworx.com/blog/choosing-the-right-kubernetes-operator-for-apache-kafka/

https://strimzi.io/blog/2019/11/05/exposing-http-bridge/

Questions:
- how to configure within azure

```
kubectl apply -f kafka-bridge.yaml -n kafka
kubectl expose svc my-bridge-bridge-service -n kafka --port=8080 --target-port=8080
kubectl port-forward -n kafka svc/my-bridge-bridge-service 8080 8080
```





