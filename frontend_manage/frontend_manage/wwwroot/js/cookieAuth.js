window.fetchWithCredentials = async (url, method, body) => {
    try {
        const options = {
            method: method,
            credentials: 'include', // This is crucial for cookies to be sent!
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        };

        // Add body for methods that need it
        if (body && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
            options.body = body;
        }

        const response = await fetch(url, options);
        
        // Log response details for debugging
        console.log(`[fetchWithCredentials] ${method} ${url} - Status: ${response.status}`);
        
        // Handle error status codes
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`[fetchWithCredentials] Error response: ${errorText}`);
            throw new Error(`HTTP error ${response.status}: ${errorText}`);
        }
        
        // Try to parse as JSON, return empty string if no content
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            const text = await response.text();
            return text || '{}'; // Return empty JSON object if no content
        } else {
            return '{}';
        }
    } catch (error) {
        console.error('[fetchWithCredentials] Error:', error);
        throw error;
    }
};
