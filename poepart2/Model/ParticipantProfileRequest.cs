namespace poepart2.Model
{
    public class ParticipantProfileRequest
    {
        public string Participantname { get; set; } = string.Empty;

        public string Participantsurname { get; set; } = string.Empty;

        public string ParticipantEmail { get; set; } = string.Empty;

        public string? ProfileImagePath { get; set; }
    }
}
