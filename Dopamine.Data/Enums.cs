namespace Dopamine.Data
{
    public enum AddFolderResult
    {
        Error = 0,
        Success = 1,
        Duplicate = 2,
        Inaccessible = 3
    }

    public enum RemoveFolderResult
    {
        Error = 0,
        Success = 1
    }

    public enum TrackOrder
    {
        Alphabetical = 1,
        ByAlbum = 2,
        ByFileName = 3,
        ByRating = 4,
        ReverseAlphabetical = 5,
        None = 6
    }

    public enum AlbumOrder
    {
        Alphabetical = 1,
        ByDateAdded = 2,
        ByDateCreated = 3,
        ByAlbumArtist = 4,
        ByYearDescending = 5,
        ByYearAscending = 6
    }

    public enum ArtistOrder
    {
        AlphabeticalAscending = 1,
        AlphabeticalDescending = 2,
        ByDateAdded = 3,
        ByDateCreated = 4,
        ByTrackCount = 5,
        ByYearDescending = 6,
        ByYearAscending = 7
    }

    public enum RemoveTracksResult
    {
        Error = 0,
        Success = 1
    }
}
