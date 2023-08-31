# NewsTrek

## Introduction 
NewsTrek is an English learning assistant web.

## Development language
* C#
* JavaScript

## Database using
* MSSQL

## Main function of the project
* Looking up vocabulary with multiple online dictionaries at the same time.
* Providing thousands of English news for users to read.
* Users can look up vocabulary that within the news immediately without switching to the other page.
* Users can input one to three vocabulary to generate a sentence by OpenAI API.
* Users can save the specific vocabulary.

## Guide
* The ElasticSearch is used to store the mass of news documents in this project.
* Please find out the route of "docker-compose.yml" file, then follow the ```docker-compose up -d``` command to install and run ElasticSearch and Kibana detached at your machine.

