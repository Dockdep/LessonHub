import json
import re
import requests
from crewai.tools import tool
from config import settings
YOUTUBE_VIDEOS_LIMIT = settings.youtube_videos_limit


def _search_youtube(query: str, limit: int = YOUTUBE_VIDEOS_LIMIT) -> list[dict]:
    """Search YouTube and extract video data from the response."""
    url = "https://www.youtube.com/results"
    params = {"search_query": query}
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
        "Accept-Language": "en-US,en;q=0.9",
    }

    response = requests.get(url, params=params, headers=headers, timeout=10)
    response.raise_for_status()

    # Extract JSON data from the HTML response
    pattern = r'var ytInitialData = ({.*?});'
    match = re.search(pattern, response.text)

    if not match:
        # Try alternative pattern
        pattern = r'ytInitialData\s*=\s*({.*?});'
        match = re.search(pattern, response.text)

    if not match:
        return []

    data = json.loads(match.group(1))

    videos = []
    try:
        contents = data["contents"]["twoColumnSearchResultsRenderer"]["primaryContents"]["sectionListRenderer"]["contents"]

        for section in contents:
            if "itemSectionRenderer" not in section:
                continue

            items = section["itemSectionRenderer"]["contents"]
            for item in items:
                if "videoRenderer" not in item:
                    continue

                video = item["videoRenderer"]
                video_id = video.get("videoId", "")
                title = video.get("title", {}).get("runs", [{}])[0].get("text", "Unknown")
                channel = video.get("ownerText", {}).get("runs", [{}])[0].get("text", "Unknown")
                duration = video.get("lengthText", {}).get("simpleText", "Unknown")

                if video_id:
                    videos.append({
                        "title": title,
                        "channel": channel,
                        "url": f"https://www.youtube.com/watch?v={video_id}",
                        "duration": duration
                    })

                if len(videos) >= limit:
                    break

            if len(videos) >= limit:
                break

    except (KeyError, IndexError, TypeError):
        pass

    return videos


@tool("YouTube Video Search")
def search_youtube_videos(query: str) -> str:
    """
    Search YouTube for videos matching the query.
    Returns a JSON string with video titles, channels, URLs, and durations.

    Args:
        query: The search query to find videos on YouTube

    Returns:
        JSON string containing list of video results
    """
    try:
        videos = _search_youtube(query, limit=YOUTUBE_VIDEOS_LIMIT)

        if not videos:
            return json.dumps({"error": "No videos found", "query": query})

        return json.dumps(videos, indent=2)
    except Exception as e:
        return json.dumps({"error": str(e), "query": query})
