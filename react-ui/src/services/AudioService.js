import api from "../http";

export default class AudioService {
    static async saveFile(blob, lessonId) {
        const data = new FormData();
        data.append('audioFile', blob);
        data.append("lessonId", lessonId)
        return api.post('audio', data, {
            headers: {'Content-Type': `multipart/form-data`}
        });
    }
}
