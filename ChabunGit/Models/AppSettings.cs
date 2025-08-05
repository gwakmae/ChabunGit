// 파일 경로: ChabunGit/Models/AppSettings.cs (수정된 코드)

namespace ChabunGit.Models
{
    /// <summary>
    /// 애플리케이션의 설정을 저장하는 데이터 모델 클래스입니다.
    /// 이 클래스의 구조가 settings.json 파일의 형식과 일치합니다.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 사용자가 마지막으로 열었던 프로젝트 폴더의 경로를 저장합니다.
        /// 프로그램 재시작 시 이 경로를 자동으로 불러오기 위해 사용됩니다.
        /// </summary>
        public string? LastOpenedFolderPath { get; set; }
    }
}