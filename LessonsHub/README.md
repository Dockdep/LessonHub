# LessonsHub - Gemini API

A simple ASP.NET Core 8.0 Web API for communicating with Google Gemini AI.

## Features

- RESTful API endpoint for sending messages to Gemini AI
- Swagger/OpenAPI documentation
- Structured request/response models
- Logging and error handling

## Prerequisites

- .NET 8.0 SDK
- Google Gemini API Key ([Get one here](https://makersuite.google.com/app/apikey))

## Configuration

1. Update your Gemini API key in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE",
    "Model": "gemini-pro"
  }
}
```

Alternatively, you can set it via environment variable or user secrets for better security:

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_API_KEY"
```

## Running the Application

1. Navigate to the project directory:
```bash
cd LessonsHub
```

2. Run the application:
```bash
dotnet run
```

3. Open your browser and navigate to:
   - Swagger UI: `https://localhost:7121` or `http://localhost:5191`

## API Endpoints

### POST /api/gemini/chat

Send messages to Gemini AI and receive a response.

**Request Body:**
```json
{
  "messages": [
    {
      "role": "user",
      "content": "Hello, how are you?"
    }
  ]
}
```

**Response:**
```json
{
  "content": "I'm doing well, thank you for asking! How can I help you today?",
  "model": "gemini-pro",
  "tokensUsed": 25
}
```

## Project Structure

```
LessonsHub/
├── Controllers/
│   └── GeminiController.cs      # API controller with POST endpoint
├── Services/
│   ├── IGeminiService.cs        # Service interface
│   └── GeminiService.cs         # Gemini communication logic
├── Models/
│   ├── GeminiRequest.cs         # Request model
│   └── GeminiResponse.cs        # Response model
├── Program.cs                    # Application entry point with Swagger config
└── appsettings.json             # Configuration file
```

## Example Usage with cURL

```bash
curl -X POST "https://localhost:7121/api/gemini/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {
        "role": "user",
        "content": "Explain what ASP.NET Core is in one sentence"
      }
    ]
  }'
```

## Technologies Used

- ASP.NET Core 8.0
- Swashbuckle.AspNetCore (Swagger)
- System.Text.Json
- HttpClient for API communication

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK` - Success
- `400 Bad Request` - Invalid request (empty messages)
- `500 Internal Server Error` - Server or Gemini API errors

## Security Notes

- Never commit your API key to source control
- Use environment variables or Azure Key Vault in production
- Consider rate limiting for production use
- Enable HTTPS in production
