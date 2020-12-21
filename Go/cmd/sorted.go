package cmd

type directorySlice []*DirectoryInfo

func (d directorySlice) Len() int {
	return len(d)
}

func (d directorySlice) Swap(i, j int) {
	d[i], d[j] = d[j], d[i]
}

func (d directorySlice) Less(i, j int) bool {
	return d[i].DirectorySize > d[j].DirectorySize
}
