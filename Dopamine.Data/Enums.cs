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
        Ranking = 6,
        Random = 7,
        None = 8
    }

    public enum AlbumOrder
    {
        AlphabeticalAscending = 1,
        AlphabeticalDescending,
        ByAlbumArtistAscending,
        ByAlbumArtistDescending,
        ByDateAdded,
        ByDateCreated,
        ByTrackCount,
        ByYearDescending,
        ByYearAscending,
        ByPlayCount
    }

    public enum ArtistOrder
    {
        AlphabeticalAscending = 1,
        AlphabeticalDescending,
        ByDateAdded ,
        ByDateCreated,
        ByTrackCount,
        ByYearDescending,
        ByYearAscending,
        ByPlayCount
    }

    public enum GenreOrder
    {
        AlphabeticalAscending = 1,
        AlphabeticalDescending,
        ByTrackCount,
        ByPlayCount
    }



    public enum RemoveTracksResult
    {
        Error = 0,
        Success = 1
    }
}
