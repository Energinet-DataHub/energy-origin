FROM rabbitmq:3.13

RUN rabbitmq-plugins enable --offline rabbitmq_management

EXPOSE 15672
