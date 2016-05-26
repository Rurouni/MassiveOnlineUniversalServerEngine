module Configuration

open FSharp.Configuration
open Argu

type Settings = AppSettings<"app.config">

type CLIArguments =
        | [<Mandatory>]Gateway_Endpoint of string
        | [<Mandatory>]User_Count of int
        | MBrace_ServiceBusConnection of string
        | MBrace_StorageConnection of string
        | Test_Duration of int
        | Messages_In_Batch of int
        | Sleep_Duration of int
        | Run_Locally
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Gateway_Endpoint _ -> "endpoint to connect to <ip_or_dns:port>"
                | User_Count _ -> "how many concurrent users to run per agent"
                | MBrace_ServiceBusConnection _ -> "binary data in base64 encoding."
                | MBrace_StorageConnection _ -> "full connection string for azure storage account that MBrace cluster is using"
                | Test_Duration _ -> "How many seconds to run the test"
                | Messages_In_Batch _ -> "How many message should single user send in one iteration"
                | Sleep_Duration _ -> "How many ms to async sleep between iterations"
                | Run_Locally _ -> "Run in local MBrace mode"

