# Performance Management

## Rate limiting

### Global rate limit

The global rate limiter will limit requests per-user (or per-IP if not logged in)
to 20 requests per second using a sliding window.

Up to 10 requests beyond the limit will be queued and served as requests become available. 

### Per endpoint rate limits

The default limits are applied using a Token-Bucket rate limiter with the following
settings:

| Endpoint type    | Token limit | Token Replenishment | Queue limit |
|------------------|-------------|---------------------|-------------|
| Listing (GET)    | 50          | 5 per second        | 10          |
| Read (GET by id) | 20          | 2 per second        | 10          |
| Create (POST)    | 2           | 1 per 3 seconds     | 0           |
| Update (PUT)     | 2           | 1 per 3 seconds     | 0           |
| Patch (PATCH)    | 10          | 1 per 2 seconds     | 0           |
| Delete (DELETE)  | 2           | 1 per 3 seconds     | 0           |

The table shows the default generic rate limits that will be applied in most cases.
However, some endpoints may implement more custom rate limiting as needed.

In all cases, if you get rate limited you will receive a HTTP 429 (Too many requests) response.
**It is the consuming application's responsibility** to control request rates and handle rate limiting gracefully.

Some key points to note:

- It's more efficient to use listing endpoints than making multiple calls to a read endpoint
- Create, Update and Delete are very restricted as they are the hardest on performance
- Patch is more efficient than Update


## Load shedding

The API implements a Load Shedding system that uses an adaptive concurrency limiter designed to drop lower priority
requests during periods of high load.

Requests that are dropped in this way will receive a 503 (Service unavailable) response. **It is the consuming application's responsibility**
to handle these responses gracefully within their own code.

The recommendation is to conditionally show/hide the relevant UI areas based on
whether you get a good response or a 503.

The animation below will help you to conceptualise how this works.

![Load shedding animation](https://farfetch.github.io/loadshedding/assets/images/adaptative_concurrency_limiter_animation-c7a45293e9c9bd494f7dc392e4093f13.gif)

For more information about how this load shedding works, see the [official documentation](https://farfetch.github.io/loadshedding/docs/guides/adaptative-concurreny-limiter/adaptative_concurrency_limiter)
