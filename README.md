This demo project shows how to perform sentiment analysis on a live Twitter feed. 


The sentiment data is exposed as Prometheus Metrics and scrapped by a Prometheus installation and stored internally as Timeseries data.  This Timeseries data can then be plotted using tools like Grafana.

The project uses a Kafka pipeline to separate ingestion and processing of data.
